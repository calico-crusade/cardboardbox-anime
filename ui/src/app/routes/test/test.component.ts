import { Component, OnInit } from '@angular/core';

@Component({
    templateUrl: './test.component.html',
    styleUrls: ['./test.component.scss']
})
export class TestComponent implements OnInit {

    page: ('magic-circle' | 'image-test') = 'magic-circle';

    squareCount: number = 3;
    rotateCount: number = 4;
    spell: string = 'Spin spin spin! Spin until you can\'t anymore!';
    maxDelay: number = 10;
    animate: boolean = true;

    get squares() { return Array(this.squareCount); }
    get rotaters() { return Array(this.rotateCount); }

    sepai: number = 100;
    hue: number = 150;
    saturate: number = 450;
    brightness: number = 100;

    constructor() { }

    ngOnInit(): void {

    }

    getDelay(index: number) {
        const per = index / this.rotaters.length;
        return (this.maxDelay * per).toFixed(2) + 's';
    }

    getStyle() {
        return `sepia(${this.sepai}%) hue-rotate(${this.hue}deg) saturate(${this.saturate}%) brightness(${this.brightness}%)`;
    }
}
