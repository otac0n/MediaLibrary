import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';

import { HashTag, SearchResult } from '../schema';
import * as StaticData from './static-data';
import { TagsService } from './tags.service';

@Injectable({
    providedIn: 'root'
})
export class TaggingService {
    constructor(
        private http: HttpClient) {
    }

    public static sort(a: string, b: string): number {
        const aTag = StaticData.allTags[a];
        const bTag = StaticData.allTags[b];
        return TagsService.sort(aTag, bTag);
    }

    public addTag(hashTag: HashTag): Promise<void>;
    public addTag(searchResult: SearchResult, tag: string): Promise<void>;
    public addTag(): Promise<void> {
        let hashTag: HashTag;
        let searchResult: SearchResult = null;
        if (arguments.length >= 2) {
            searchResult = arguments[0];
            hashTag = {
                hash: searchResult.hash,
                tag: arguments[1],
            };
        } else {
            hashTag = arguments[0];
        }

        const promise = this.http.put<void>(`files/${encodeURIComponent(hashTag.hash)}/tags/${encodeURIComponent(hashTag.tag)}`, {}).toPromise();
        if (!!searchResult && searchResult.tags.indexOf(hashTag.tag) === -1) {
            searchResult.tags.push(hashTag.tag);
            searchResult.tags.sort(TaggingService.sort);
        }

        return promise;
    }

    public removeTag(hashTag: HashTag): Promise<void>;
    public removeTag(searchResult: SearchResult, tag: string): Promise<void>;
    public removeTag(): Promise<void> {
        let hashTag: HashTag;
        let searchResult: SearchResult = null;
        if (arguments.length >= 2) {
            searchResult = arguments[0];
            hashTag = {
                hash: searchResult.hash,
                tag: arguments[1],
            };
        } else {
            hashTag = arguments[0];
        }

        const promise = this.http.delete<void>(`files/${encodeURIComponent(hashTag.hash)}/tags/${encodeURIComponent(hashTag.tag)}`).toPromise();
        searchResult.tags.splice(searchResult.tags.indexOf(hashTag.tag), 1);

        return promise;
    }
}
