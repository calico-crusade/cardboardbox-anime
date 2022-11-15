import { Component, OnInit } from '@angular/core';

@Component({
    templateUrl: './test.component.html',
    styleUrls: ['./test.component.scss']
})
export class TestComponent implements OnInit {

    squareCount: number = 3;
    rotateCount: number = 4;
    spell: string = 'Spin spin spin! Spin until you can\'t anymore!';
    maxDelay: number = 10;
    animate: boolean = true;

    get squares() { return Array(this.squareCount); }
    get rotaters() { return Array(this.rotateCount); }

    constructor() { }

    ngOnInit(): void {

    }

    getDelay(index: number) {
        const per = index / this.rotaters.length;
        return (this.maxDelay * per).toFixed(2) + 's';
    }
}
