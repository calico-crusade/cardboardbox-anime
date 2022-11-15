import { Component, OnInit } from '@angular/core';

@Component({
    templateUrl: './test.component.html',
    styleUrls: ['./test.component.scss']
})
export class TestComponent implements OnInit {

    squares: number[] = Array(3);

    constructor() { }

    ngOnInit(): void {

    }

}
