import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';

import { TagsService } from './tags.service';
import { SearchResult } from '../schema';
import * as StaticData from './static-data';

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
            });
            return results;
        }));
    }
}
