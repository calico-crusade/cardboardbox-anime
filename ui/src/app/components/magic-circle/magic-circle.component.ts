import { Component, Input, OnInit } from '@angular/core';

@Component({
    selector: 'magic-circle',
    templateUrl: './magic-circle.component.html',
    styleUrls: ['./magic-circle.component.scss']
})
export class MagicCircleComponent implements OnInit {

    @Input() width: string = '100%';
    @Input() height: string = '100%';
    @Input('spin-speed') spinSpeed?: string;

    constructor() { }

    ngOnInit(): void {
    }

    style() {
        const animate = this.spinSpeed ? `animation-duration: ${this.spinSpeed};` : '';

        return `width: calc(${this.width} - 2px); height: calc(${this.height} - 2px); ${animate}`;
    }

}
