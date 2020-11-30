import { Component, ElementRef, Input, OnChanges, OnInit, SimpleChanges, ViewChild } from '@angular/core';

import { SearchResult } from '../../schema';
import { TaggingService } from '../tagging.service';

@Component({
  selector: 'app-tag-list-editor',
  templateUrl: './tag-list-editor.component.html',
  styleUrls: ['./tag-list-editor.component.scss']
})
export class TagListEditorComponent implements OnChanges, OnInit {
    @Input()
    public searchResult: SearchResult;

    @Input()
    public excludeFavorite = false;

    @ViewChild('tagInput')
    public tagInput: ElementRef;

    public inputValue = '';

    private editStack: string[] = [];

    constructor(
        private taggingService: TaggingService) {
    }

    ngOnChanges(changes: SimpleChanges) {
        if ('searchResult' in changes) {
            this.editStack = [];
            this.inputValue = '';
        }
    }

    ngOnInit(): void {
    }

    public addTag(tag: string) {
        const ix = this.editStack.indexOf(tag);
        if (ix !== -1) {
            this.editStack.splice(ix, 1);
        }

        this.editStack.push(tag);

        this.taggingService.addTag(this.searchResult, tag);
    }

    public removeTag(tag: string) {
        const ix = this.editStack.indexOf(tag);
        if (ix !== -1) {
            this.editStack.splice(ix, 1);
        }

        this.taggingService.removeTag(this.searchResult, tag);
    }

    public editTag(tag: string) {
        this.removeTag(tag);
        this.inputValue = tag;
    }

    public rejectTag(tag: string) {
        this.removeTag(tag);
    }

    public click(event: MouseEvent) {
        event.stopPropagation();
    }

    public keydown(event: KeyboardEvent) {
        switch (event.which) {
            case 8:
                if (this.inputValue === '') {
                    if (this.editStack.length > 0) {
                        this.editTag(this.editStack.pop());
                        event.preventDefault();
                        event.stopPropagation();
                    }
                } else {
                    const input = this.tagInput.nativeElement;
                    const start = input.selectionStart;
                    const end = input.selectionEnd;
                    if (start === end && start === 0) {
                        input.select();
                        event.preventDefault();
                        event.stopPropagation();
                    }
                }
                break;
        }
    }

    public keypress(event: KeyboardEvent) {
        switch (event.which) {
            case 32:
                // TODO: Handle different cursor positions.
                const tags = this.inputValue.split(' ').filter(t => !!t).forEach(t => {
                    this.addTag(t);
                });
                this.inputValue = '';
                event.preventDefault();
                event.stopPropagation();

                break;
        }
    }
}
