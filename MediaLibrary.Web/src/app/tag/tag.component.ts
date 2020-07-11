import { Component, EventEmitter, HostListener, Input, OnInit, Output } from '@angular/core';

@Component({
    selector: 'app-tag',
    templateUrl: './tag.component.html',
    styleUrls: ['./tag.component.scss']
})
export class TagComponent implements OnInit {
    @Input()
    public tag: string;

    @Input()
    public allowDelete = false;

    @Output()
    public remove = new EventEmitter<void>();

    @Output()
    public edit = new EventEmitter<void>();

    @Output()
    public reject = new EventEmitter<void>();

    constructor() {
    }

    ngOnInit(): void {
    }

    @HostListener('click', ['$event'])
    click(event: MouseEvent) {
        if (this.allowDelete) {
            event.stopPropagation();
        }
    }

    @HostListener('dblclick', ['$event'])
    dblclick(event: MouseEvent) {
        if (this.allowDelete) {
            this.edit.emit();
            event.preventDefault();
            event.stopPropagation();
        }
    }

    @HostListener('keydown', ['$event'])
    public keypress(event: KeyboardEvent) {
        if (this.allowDelete) {
            switch (event.which) {
                case 8:
                    this.remove.emit();
                    event.preventDefault();
                    event.stopPropagation();
                    break;

                case 46:
                    this.remove.emit();
                    event.preventDefault();
                    event.stopPropagation();
                    break;
            }
        }
    }
}
