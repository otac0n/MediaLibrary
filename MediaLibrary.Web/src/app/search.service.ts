import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { SearchResult } from '../schema';

@Injectable({
    providedIn: 'root'
})
export class SearchService {
    constructor(
        private http: HttpClient) {
    }

    public search(q: string): Observable<SearchResult[]> {
        return this.http.get<SearchResult[]>(`files?q=${encodeURIComponent(q)}`);
    }
}
