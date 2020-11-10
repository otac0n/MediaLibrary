import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';

import { SavedSearch } from '../schema';

@Injectable({
    providedIn: 'root'
})
export class SavedSearchService {
    constructor(
        private http: HttpClient) {
    }

    public list(): Promise<SavedSearch[]> {
        return this.http.get<SavedSearch[]>('searches').toPromise();
    }
}
