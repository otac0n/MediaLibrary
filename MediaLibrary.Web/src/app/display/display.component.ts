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

    public get isImage(): boolean {
        return this.displayResult?.fileType === 'image' || this.displayResult?.fileType.startsWith('image/')
    }

    public get isVideo(): boolean {
        return this.displayResult?.fileType === 'video' || this.displayResult?.fileType.startsWith('video/')
    }

    constructor(
        private changeDetector: ChangeDetectorRef) {
    }

    public stopClick(event: MouseEvent): void {
        // Tapping a video (or perhaps its built-in controls) should not dismiss the video.
        event.stopPropagation();
    }

    public ngOnChanges(changes: SimpleChanges) {
        const searchResult = changes.searchResult;
        if (searchResult) {
            // Destroy the current image or video control by queueing an update to set it only on the next frame.
            // This avoids keeping the old image or video visible while loading the next.
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
