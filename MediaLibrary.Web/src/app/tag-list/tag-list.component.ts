import { Component, ElementRef, EventEmitter, Input, OnInit, Output, ViewChild } from '@angular/core';

@Component({
    selector: 'app-tag-list',
    templateUrl: './tag-list.component.html',
    styleUrls: ['./tag-list.component.scss']
})
export class TagListComponent implements OnInit {
    @Input()
    public tags: string[];

    @Input()
    public excludeFavorite = false;

    @Input()
    public editable = false;

    @Output()
    public addTag = new EventEmitter<string>();

    @Output()
    public removeTag = new EventEmitter<string>();

    @Output()
    public rejectTag = new EventEmitter<string>();

    @ViewChild('tagInput')
    public tagInput: ElementRef;

    public inputValue = '';

    constructor() {
    }

    public click(event: MouseEvent) {
        event.stopPropagation();
    }

    public editTag(tag: string) {
        this.removeTag.emit(tag);
        this.inputValue = tag;
    }

    public keydown(event: KeyboardEvent) {
        switch (event.which) {
            case 8:
                if (this.editable) {
                    if (this.inputValue === '') {
                        if (this.tags.length > 0) {
                            this.inputValue = this.tags.pop();
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
                }

                break;
        }
    }

    public keypress(event: KeyboardEvent) {
        switch (event.which) {
            case 32:
                // TODO: Handle different cursor positions.
                if (this.editable) {
                    const tags = this.inputValue.split(' ').filter(t => !!t).forEach(t => {
                        this.addTag.emit(t);
                    });
                    this.inputValue = '';
                    event.preventDefault();
                    event.stopPropagation();
                }

                break;
        }
    }

    ngOnInit(): void {
    }
}
