import { Component, EventEmitter, HostListener, Input, Output } from '@angular/core';

import { SearchResult } from '../../schema';
import { TaggingService } from '../tagging.service';

interface SwipeHandlers {
    left?: () => void;
    right?: () => void;
    up?: () => void;
    down?: () => void;
}

class SwipeDetector {
    public swipeMinDistance = 30;
    public swipeMaxTime = 500;
    private touchStart: { x: number, y: number, t: Date } = null;

    public handleTouchStart(event: TouchEvent) {
        if (event.changedTouches.length === 1) {
            this.touchStart = {
                x: event.changedTouches[0].screenX,
                y: event.changedTouches[0].screenY,
                t: new Date(),
            };
        } else {
            this.touchStart = null;
        }
    }

    public handleTouchEnd(event: TouchEvent, swipeHandlers: SwipeHandlers) {
        if (this.touchStart && (new Date().getTime() - this.touchStart.t.getTime()) < this.swipeMaxTime) {
            const dx = event.changedTouches[0].screenX - this.touchStart.x;
            const dy = event.changedTouches[0].screenY - this.touchStart.y;

            if ((dx * dx) + (dy * dy) > (this.swipeMinDistance * this.swipeMinDistance)) {
                const horizontal = Math.abs(dx) >= Math.abs(dy) * 1.5 ? Math.sign(dx) : 0;
                const vertical = Math.abs(dy) >= Math.abs(dx) * 1.5 ? Math.sign(dy) : 0;
                switch (horizontal + ',' + vertical) {
                    case '1,0':
                        swipeHandlers?.right();
                        break;
                    case '-1,0':
                        swipeHandlers?.left();
                        break;
                    case '0,1':
                        swipeHandlers?.down();
                        break;
                    case '0,-1':
                        swipeHandlers?.up();
                        break;
                }
            }
        }

        this.touchStart = null;
    }
}

@Component({
    selector: 'app-preview',
    templateUrl: './preview.component.html',
    styleUrls: ['./preview.component.scss']
})
export class PreviewComponent {
    @Input()
    public searchResult: SearchResult;

    @Output()
    public requestClose = new EventEmitter<void>();

    @Output()
    public requestNext = new EventEmitter<void>();

    @Output()
    public requestPrevious = new EventEmitter<void>();

    private swipeDetector = new SwipeDetector();

    constructor(
        private taggingService: TaggingService) {
    }

    public onClick(): void {
        this.requestClose.emit();
    }

    public favorite() {
        this.taggingService.addTag(this.searchResult, 'favorite');
    }

    public unfavorite() {
        this.taggingService.removeTag(this.searchResult, 'favorite');
    }

    @HostListener('touchstart', ['$event'])
    public handleTouchStart(event: TouchEvent) {
        this.swipeDetector.handleTouchStart(event);
    }

    @HostListener('touchend', ['$event'])
    public handleTouchEnd(event: TouchEvent) {
        this.swipeDetector.handleTouchEnd(event, {
            left: () => this.requestNext.emit(),
            right: () => this.requestPrevious.emit(),
        });
    }

    @HostListener('document:keydown', ['$event'])
    public keyDown(event: KeyboardEvent) {
        if (this.searchResult) {
            switch (event.which) {
                case 27:
                    this.requestClose.emit();
                    event.preventDefault();
                    event.stopPropagation();
                    break;

                case 37:
                    this.requestPrevious.emit();
                    event.preventDefault();
                    event.stopPropagation();
                    break;

                case 39:
                    this.requestNext.emit();
                    event.preventDefault();
                    event.stopPropagation();
                    break;
            }
        }
    }
}
