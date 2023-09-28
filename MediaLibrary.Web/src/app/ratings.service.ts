import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';

import { Rating } from '../schema';

export type StarRange = [number | null, number | null];

function isRating(value: number | Rating): value is Rating {
    return typeof value === 'object';
}

@Injectable({
    providedIn: 'root'
})
export class RatingsService {
    public static readonly DefaultRating = 1500;

    constructor(
        private http: HttpClient) { }

    public getAllRatingCategories(): Promise<string[]> {
        return this.http.get<string[]>('ratings/categories').toPromise();
    }

    public getRatingStarRanges(): Promise<StarRange[]> {
        return this.http.get<StarRange[]>('ratings/stars').toPromise();
    }

    public get(id: string, category: string): Promise<Rating> {
        const uri = category
            ? `ratings/categories/${encodeURIComponent(category)}/files/${encodeURIComponent(id)}`
            : `ratings/files/${encodeURIComponent(id)}`;
        return this.http.get<Rating>(uri).toPromise();
    }

    public getExpectedScore(left: number | Rating, right: number | Rating) {
        if (isRating(left)) {
            left = left.value;
        }

        if (isRating(right)) {
            right = right.value;
        }

        return 1.0 / (1.0 + Math.pow(10.0, (left - right) / 400.0));
    }

    public rate(score: number, left: Rating, right: Rating): Promise<void> {
        const leftCategory = left.category || '';
        const rightCategory = right.category || '';
        if (leftCategory !== rightCategory) {
            throw new Error(`Rating categories must match. Cannot rate across the categories '${left.category}' and '${right.category}'`);
        }

        const uri = leftCategory
            ? `ratings/categories/${encodeURIComponent(left.category)}/rate`
            : `ratings/rate`;
        return this.http.post<Rating[]>(uri, { score, leftHash: left.hash, rightHash: right.hash }).toPromise().then(updates => {
            Object.assign(left, updates[0]);
            Object.assign(right, updates[1]);
        });
    }
}
