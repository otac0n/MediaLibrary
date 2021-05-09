import { ChangeDetectorRef, Component, EventEmitter, Input, OnChanges, Output, SimpleChanges } from '@angular/core';

import { Rating, SearchResult } from '../../schema';
import { RatingsService } from '../ratings.service';

interface ItemInfo {
    index: number;
    rating: Rating;
}

@Component({
    selector: 'app-compare',
    templateUrl: './compare.component.html',
    styleUrls: ['./compare.component.scss']
})
export class CompareComponent implements OnChanges {
    @Input()
    public category: string;

    @Input()
    public searchResults: SearchResult[];

    @Output()
    public requestClose = new EventEmitter<void>();

    public score: number;
    public leftItem: ItemInfo;
    public rightItem: ItemInfo;

    constructor(
        private ratingsService: RatingsService,
        private changeDetector: ChangeDetectorRef) {
        this.resetComparison();
    }

    public ngOnChanges(changes: SimpleChanges) {
        if (changes.searchResults) {
            if (this.searchResults && this.searchResults.length > 1) {
                this.loadNextComparison().then(() => this.changeDetector.detectChanges());
            } else {
                this.resetComparison();
            }
        }
    }

    public close(): void {
        this.requestClose.emit();
    }

    public async rate(): Promise<void> {
        await this.ratingsService.rate(this.score, this.leftItem.rating, this.rightItem.rating);
        await this.loadNextComparison();
    }

    public async skip(): Promise<void> {
        await this.loadNextComparison();
    }

    private async loadNextComparison(): Promise<void> {
        this.resetComparison();

        const leftIx = this.randomIndex();
        const rightIx = this.randomIndex(leftIx);
        const leftResult = this.searchResults[leftIx];
        const rightResult = this.searchResults[rightIx];
        const leftRating = await this.ratingsService.get(leftResult.hash, this.category) || { hash: leftResult.hash, category: this.category, value: RatingsService.DefaultRating, count: 0 };
        const rightRating = await this.ratingsService.get(rightResult.hash, this.category) || { hash: rightResult.hash, category: this.category, value: RatingsService.DefaultRating, count: 0 };
        this.leftItem = { index: leftIx, rating: leftRating };
        this.rightItem = { index: rightIx, rating: rightRating };
        this.score = this.ratingsService.getExpectedScore(leftRating.value, rightRating.value);
    }

    private resetComparison() {
        this.leftItem = null;
        this.rightItem = null;
        this.score = 0.5;
    }

    private randomIndex(avoid?: number): number {
        if (avoid === undefined) {
            return Math.floor(Math.random() * this.searchResults.length);
        }

        let ix = Math.floor(Math.random() * (this.searchResults.length - 1));
        if (ix >= avoid) {
            ix += 1;
        }

        return ix;
    }
}
