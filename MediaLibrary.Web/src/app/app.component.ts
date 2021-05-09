import { Component, OnInit } from '@angular/core';
import { filter } from 'rxjs/operators';

import { SavedSearch, SearchResult } from '../schema';
import { RatingsService } from './ratings.service';
import { SavedSearchService } from './saved-search.service';
import { SearchService } from './search.service';
import { TaggingService } from './tagging.service';

@Component({
    selector: 'app-root',
    templateUrl: './app.component.html',
    styleUrls: ['./app.component.scss']
})
export class AppComponent implements OnInit {
    public ratingCategories: string[];
    public savedSearches: SavedSearch[];
    public previewItem: boolean;
    public query = '';
    public ratingCategory: string;
    public selectedItem: SearchResult;
    public selectedHashes: { [hash: string]: boolean } = {};
    public resultsPage: SearchResult[] = [];
    public compareResults: SearchResult[];

    private searchVersion = 0;

    constructor(
        private ratingsService: RatingsService,
        private savedSearchService: SavedSearchService,
        private searchService: SearchService,
        private taggingService: TaggingService) {
    }

    public ngOnInit() {
        this.savedSearchService.list().then(data => {
            data.sort((a, b) => (a?.name || '').localeCompare(b?.name || ''));
            this.savedSearches = data;
        });
        this.ratingsService.getAllRatingCategories().then(data => {
            this.ratingCategories = data;
        });
    }

    public searchKeyPress(event: KeyboardEvent) {
        switch (event.which) {
            case 13:
                this.search();
                event.preventDefault();
                event.stopPropagation();
                break;
        }
    }

    public search() {
        const version = ++this.searchVersion;
        this.resultsPage = [];
        this.selectedItem = null;
        this.previewItem = false;
        this.searchService.search(this.query)
            .pipe(filter(_ => version === this.searchVersion))
            .toPromise()
            .then(results => {
                const newSelected = {};
                results.filter(r => this.selectedHashes[r.hash]).forEach(r => newSelected[r.hash] = true);

                this.resultsPage = results;
                this.selectedHashes = newSelected;
            });
    }

    public select(result: SearchResult) {
        this.selectedItem = result;
        this.previewItem = true;
    }

    public toggleCheck(event: Event & { target: HTMLInputElement }, result: SearchResult) {
        if (event.target.checked) {
            this.selectedHashes[result.hash] = true;
        } else {
            delete this.selectedHashes[result.hash];
        }
    }

    public stopClick(event: MouseEvent) {
        event.stopPropagation();
    }

    public favorite(result: SearchResult) {
        this.taggingService.addTag({ hash: result.hash, tag: 'favorite' });
        if (result.tags.indexOf('favorite') > -1) {
            result.tags.unshift('favorite');
        }
    }

    public unfavorite(result: SearchResult) {
        this.taggingService.removeTag({ hash: result.hash, tag: 'favorite' });
        result.tags.splice(result.tags.indexOf('favorite'), 1);
    }

    public closeCompare() {
        this.ratingCategory = null;
        this.compareResults = null;
    }

    public closePreview() {
        this.previewItem = false;
    }

    public next() {
        this.move(1);
    }

    public previous() {
        this.move(-1);
    }

    public rate(category: string) {
        this.ratingCategory = category;
        this.compareResults = this.getSelectedItems();
    }

    private getSelectedItems(): SearchResult[] {
        let selectedResults = this.resultsPage.filter(r => this.selectedHashes[r.hash]);
        if (selectedResults.length === 0) {
            selectedResults = this.resultsPage;
        }

        return selectedResults;
    }

    private move(dir: number) {
        if (this.selectedItem) {
            this.selectedItem = this.resultsPage[this.resultsPage.indexOf(this.selectedItem) + dir];
        }
    }
}
