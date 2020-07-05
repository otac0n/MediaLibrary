import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';

import { HashTag } from '../schema';

@Injectable({
    providedIn: 'root'
})
export class TaggingService {
    constructor(
        private http: HttpClient) {
    }

    public addTag(hashTag: HashTag): Promise<void> {
        return this.http.put<void>(`files/${encodeURIComponent(hashTag.hash)}/tags/${encodeURIComponent(hashTag.tag)}`, {}).toPromise();
    }

    public removeTag(hashTag: HashTag): Promise<void> {
        return this.http.delete<void>(`files/${encodeURIComponent(hashTag.hash)}/tags/${encodeURIComponent(hashTag.tag)}`).toPromise();
    }
}
