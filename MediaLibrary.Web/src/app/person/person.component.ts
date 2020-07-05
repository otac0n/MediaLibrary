import { Component, Input, OnInit } from '@angular/core';

import { Person } from '../../schema';

@Component({
    selector: 'app-person',
    templateUrl: './person.component.html',
    styleUrls: ['./person.component.scss']
})
export class PersonComponent implements OnInit {
    @Input()
    public person: Person;

    constructor() {
    }

    ngOnInit(): void {
    }
}
