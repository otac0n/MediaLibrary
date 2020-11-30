import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';

import { SearchResult } from '../schema';
import { TaggingService } from './tagging.service';

@Injectable({
    providedIn: 'root'
})
export class SearchService {
    constructor(
        private http: HttpClient) {
    }

    public static sortPaths(a: string, b: string): number {
        if (a === b) {
            return 0;
        } else if (a === null) {
            return -1;
        } else if (b === null) {
            return 1;
        }

        const aParts = a.toUpperCase().split(/[\\/]+/g);
        const bParts = b.toUpperCase().split(/[\\/]+/g);

        for (let j = 0; j < aParts.length && j < bParts.length; j++) {
            if (aParts.length !== bParts.length) {
                if (j === aParts.length - 1) {
                    return 1;
                } else if (j === bParts.length - 1) {
                    return -1;
                }
            }

            const num = aParts[j].localeCompare(bParts[j]);
            if (num !== 0) {
                return num;
            }
        }

        return 0;
    }

    public search(q: string): Observable<SearchResult[]> {
        return this.http.get<SearchResult[]>(`files?q=${encodeURIComponent(q)}`).pipe(map(results => {
            results.forEach(r => {
                r.tags.sort(TaggingService.sort);
                r.paths.sort(SearchService.sortPaths);
            });
            return results;
        }));
    }
}
