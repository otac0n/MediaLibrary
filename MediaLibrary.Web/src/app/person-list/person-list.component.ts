import { Component, Input } from '@angular/core';

import { Person } from '../../schema';

@Component({
    selector: 'app-person-list',
    templateUrl: './person-list.component.html',
    styleUrls: ['./person-list.component.scss']
})
export class PersonListComponent {
    @Input()
    public people: Person[];
}
