import { Component, OnInit } from '@angular/core';
import { filter } from 'rxjs/operators';

import { SavedSearch, SearchResult } from '../schema';
import { SavedSearchService } from './saved-search.service';
import { SearchService } from './search.service';
import { TaggingService } from './tagging.service';

@Component({
    selector: 'app-root',
    templateUrl: './app.component.html',
    styleUrls: ['./app.component.scss']
})
export class AppComponent implements OnInit {
    public savedSearches: SavedSearch[];
    public previewItem: boolean;
    public query = '';
    public selectedItem: SearchResult;
    public resultsPage: SearchResult[] = [];

    private searchVersion = 0;

    constructor(
        private savedSearchService: SavedSearchService,
        private searchService: SearchService,
        private taggingService: TaggingService) {
    }

    public ngOnInit() {
        this.savedSearchService.list().then(data => {
            data.sort((a, b) => (a?.name || '').localeCompare(b?.name || ''));
            this.savedSearches = data;
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
                this.resultsPage = results;
            });
    }

    public select(result: SearchResult) {
        this.selectedItem = result;
        this.previewItem = true;
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

    public close() {
        this.previewItem = false;
    }

    public previous() {
        this.move(-1);
    }

    public next() {
        this.move(1);
    }

    private move(dir: number) {
        if (this.selectedItem) {
            this.selectedItem = this.resultsPage[this.resultsPage.indexOf(this.selectedItem) + dir];
        }
    }
}
