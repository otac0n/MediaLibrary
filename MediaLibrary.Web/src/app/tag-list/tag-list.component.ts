import { Component, EventEmitter, Input, Output } from '@angular/core';

@Component({
    selector: 'app-tag-list',
    templateUrl: './tag-list.component.html',
    styleUrls: ['./tag-list.component.scss']
})
export class TagListComponent {
    @Input()
    public tags: string[];

    @Input()
    public excludeFavorite = false;

    @Input()
    public editable = false;

    @Output()
    public editTag = new EventEmitter<string>();

    @Output()
    public removeTag = new EventEmitter<string>();

    @Output()
    public rejectTag = new EventEmitter<string>();
}
