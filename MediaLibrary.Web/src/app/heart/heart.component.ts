import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';

@Component({
    selector: 'app-heart',
    templateUrl: './heart.component.html',
    styleUrls: ['./heart.component.scss'],
})
export class HeartComponent implements OnInit {
    @Input()
    public favorite = false;

    public stillHover = false;

    @Output()
    public favorited = new EventEmitter<void>();

    @Output()
    public unfavorited = new EventEmitter<void>();

    constructor() {
    }

    ngOnInit(): void {
    }

    onClick(event: MouseEvent): void {
        this.favorite = !this.favorite;
        this.stillHover = true;
        event.stopPropagation();
        event.preventDefault();
        this.favorite ? this.favorited.emit() : this.unfavorited.emit();
    }

    onLeave(): void {
        this.stillHover = false;
    }
}
