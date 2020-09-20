import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';

import { SearchResult } from '../schema';
import * as StaticData from './static-data';
import { TagsService } from './tags.service';

@Injectable({
    providedIn: 'root'
})
export class SearchService {
    constructor(
        private http: HttpClient,
        private tagsService: TagsService) {
    }

    public search(q: string): Observable<SearchResult[]> {
        return this.http.get<SearchResult[]>(`files?q=${encodeURIComponent(q)}`).pipe(map(results => {
            results.forEach(r => {
                r.tags.sort((a, b) => {
                    const aTag = StaticData.alltags[a];
                    const bTag = StaticData.alltags[b];
                    return TagsService.sort(aTag, bTag);
                });
                r.paths.sort(SearchService.sortPaths);
            });
            return results;
        }));
    }

    public static sortPaths(a: string, b: string): number {
        if (a == b) {
            return 0;
        } else if (a == null) {
            return -1;
        } else if (b == null) {
            return 1;
        }

        var aParts = a.toUpperCase().split(/[\\/]+/g);
        var bParts = b.toUpperCase().split(/[\\/]+/g);

        for (var j = 0; j < aParts.length && j < bParts.length; j++) {
            if (aParts.length != bParts.length) {
                if (j == aParts.length - 1) {
                    return 1;
                } else if (j == bParts.length - 1) {
                    return -1;
                }
            }

            var num;
            if ((num = aParts[j].localeCompare(bParts[j])) != 0) {
                return num;
            }
        }

        return 0;
    }
}
