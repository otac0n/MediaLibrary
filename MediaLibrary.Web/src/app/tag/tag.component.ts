import { Component, EventEmitter, HostListener, Input, OnInit, Output } from '@angular/core';

import { ColorService } from '../color.service';
import * as StaticData from '../static-data';
import { TagsService } from '../tags.service';

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

    public style;

    constructor(
        private colorService: ColorService) {
    }

    ngOnInit(): void {
        const tag = StaticData.allTags[this.tag];
        if (tag) {
            const color = TagsService.getColor(tag);
            if (color) {
                const parsed = this.colorService.parseColor(color);
                if (parsed) {
                    const contrast = this.colorService.contrastColor(color);
                    this.style = { 'background-color': color, color: contrast };
                }
            }
        }
    }

    @HostListener('click', ['$event'])
    public click(event: MouseEvent) {
        if (this.allowDelete) {
            event.stopPropagation();
        }
    }

    @HostListener('dblclick', ['$event'])
    public dblclick(event: MouseEvent) {
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
