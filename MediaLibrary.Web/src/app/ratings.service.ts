import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';

import { Rating } from '../schema';

@Injectable({
    providedIn: 'root'
})
export class RatingsService {
    public static readonly DefaultRating = 1500;

    constructor(
        private http: HttpClient) { }

    public getAllRatingCategories(): Promise<string[]> {
        return this.http.get<string[]>('ratings').toPromise();
    }

    public get(id: string, category: string): Promise<Rating> {
        return this.http.get<Rating>(`ratings/${encodeURIComponent(category)}/files/${encodeURIComponent(id)}`).toPromise();
    }

    public getExpectedScore(left: number | Rating, right: number | Rating) {
        if (typeof left === 'object') {
            left = left.value;
        }

        if (typeof right === 'object') {
            right = right.value;
        }

        return 1.0 / (1.0 + Math.pow(10.0, (left - right) / 400.0));
    }

    public rate(score: number, left: Rating, right: Rating): Promise<void> {
        if (left.category !== right.category) {
            throw new Error(`Rating categories must match. Cannot rate across the categories '${left.category}' and '${right.category}'`);
        }

        return this.http.post<void>(`ratings/${encodeURIComponent(left.category)}`, { score, leftHash: left.hash, rightHash: right.hash }).toPromise();
    }
}
