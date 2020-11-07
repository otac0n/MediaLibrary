import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';

import { TagInfo, TagInfoTransition } from '../schema';

export type TagSet = { [tag: string]: TagInfo };

@Injectable({
    providedIn: 'root'
})
export class TagsService {
    private allTagsPromise: Promise<TagSet>;

    constructor(
        private http: HttpClient) {
    }

    public static getColor(tag: TagInfo): string {
        return TagsService.getProperty(tag, 'color')?.trim();
    }

    public static getOrder(tag: TagInfo): number {
        const order = TagsService.getProperty(tag, 'order')?.trim();
        const value = order === '' || order === null ? 100 : +order;
        return isNaN(value) ? 100 : value;
    }

    public static getProperty(tag: TagInfo, name: string): string {
        const found = TagsService.findProperty(tag, name + '=');
        return found ? found.substr(name.length + 1) : null;
    }

    public static findProperty(tag: TagInfo, prefix: string): string;
    public static findProperty(tag: TagInfo, isMatch: (property: string) => boolean): string;
    public static findProperty(tag: TagInfo, filter: string | ((property: string) => boolean)): string {
        if (!tag) {
            return null;
        }

        if (typeof filter === 'string') {
            const prefix = filter;
            filter = property => property.startsWith(prefix);
        }

        const visited: { [tag: string]: boolean } = {};
        const queue = [tag];
        while (queue.length > 0) {
            const candidate = queue.shift();
            if (!visited[candidate.tag]) {
                visited[candidate.tag] = true;
                for (const property of candidate.properties) {
                    if (filter(property)) {
                        return property;
                    }
                }
                Array.prototype.push.apply(queue, candidate.parents);
            }
        }
    }

    public static sort(a: TagInfo, b: TagInfo): number {
        let comp = 0;

        const aOrder = TagsService.getOrder(a);
        const bOrder = TagsService.getOrder(b);
        comp = aOrder - bOrder;
        if (comp !== 0) {
            return comp;
        }

        const aColor = TagsService.getColor(a);
        const bColor = TagsService.getColor(b);

        if (aColor && bColor) {
            comp = aColor.localeCompare(bColor);
            if (comp !== 0) {
                return comp;
            }
        } else if (aColor && !bColor) {
            return -1;
        } else if (bColor && !aColor) {
            return 1;
        }

        const aDescendants = a && a.descendants.length || 0;
        const bDescendants = b && b.descendants.length || 0;
        comp = bDescendants - aDescendants;
        if (comp !== 0) {
            return comp;
        }

        return (a?.tag || '').localeCompare(b?.tag || '');
    }

    public async allTags(): Promise<TagSet> {
        const tags = await this.http.get<TagInfoTransition[]>(`tags`).toPromise();
        const results: { [tag: string]: TagInfo } = {};
        tags.forEach(tag => {
            results[tag.tag] = tag as TagInfo;
            tag.aliases.forEach(alias => results[alias] = tag as TagInfo);
        });
        const lookup = (tag: string) => results[tag];
        tags.forEach(tag => {
            tag.parents = (tag.parents as string[]).map(lookup);
            tag.children = (tag.children as string[]).map(lookup);
            tag.ancestors = (tag.ancestors as string[]).map(lookup);
            tag.descendants = (tag.descendants as string[]).map(lookup);
        });
        return results;
    }
}
