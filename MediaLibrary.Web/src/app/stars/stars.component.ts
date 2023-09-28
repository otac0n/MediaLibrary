import { Component, Input, OnInit } from '@angular/core';

import { RatingsService, StarRange } from '../ratings.service';

@Component({
  selector: 'app-stars',
  templateUrl: './stars.component.html',
  styleUrls: ['./stars.component.scss']
})
export class StarsComponent {
    @Input()
    public rating: number;

    @Input()
    public starRanges: StarRange[];

    public get defaultRating(): number {
        return RatingsService.DefaultRating;
    }

    constructor() {
    }
}
