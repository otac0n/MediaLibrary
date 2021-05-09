import { ChangeDetectorRef, Component, Input, OnChanges, SimpleChanges } from '@angular/core';

import { SearchResult } from '../../schema';

@Component({
    selector: 'app-display',
    templateUrl: './display.component.html',
    styleUrls: ['./display.component.scss']
})
export class DisplayComponent implements OnChanges {
    @Input()
    public searchResult: SearchResult;

    public displayResult: SearchResult;

    constructor(
        private changeDetector: ChangeDetectorRef) {
    }

    public ngOnChanges(changes: SimpleChanges) {
        const searchResult = changes.searchResult;
        if (searchResult) {
            this.displayResult = null;
            if (searchResult.currentValue) {
                setTimeout(() => {
                    if (this.searchResult === searchResult.currentValue) {
                        this.displayResult = this.searchResult;
                        this.changeDetector.detectChanges();
                    }
                }, 0);
            }
        }
    }
}
