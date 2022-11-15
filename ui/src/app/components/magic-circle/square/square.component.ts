import { Component, OnInit, Input } from '@angular/core';

@Component({
    selector: 'mc-square',
    templateUrl: './square.component.html',
    styleUrls: ['./square.component.scss']
})
export class SquareComponent implements OnInit {

    @Input() width: number = 100;
    @Input() height: number = 100;
    @Input() color: string = '#fff';
    @Input() rotate?: number;

    @Input() total?: number;
    @Input() index?: number;

    get deg() {
        if (this.rotate) return this.rotate;

        if (!this.total || !this.index) return 0;

        return 90 / this.total * this.index;
    }

    constructor() { }

    ngOnInit(): void {
    }

    style() {
        const w = (Math.sqrt(Math.pow(this.width, 2) + Math.pow(this.height, 2)) / 2); 
        return `width: calc(${w}% - 10px); 
        height: calc(${w}% - 10px);
        transform: translate(-50%, -50%) rotate(${this.deg}deg);
        transform-origin: center center`;
    }
}
