import { Component, Input } from '@angular/core';

import { SearchResult } from '../../schema';

@Component({
  selector: 'app-display',
  templateUrl: './display.component.html',
  styleUrls: ['./display.component.scss']
})
export class DisplayComponent {
    @Input()
    public searchResult: SearchResult;
}
