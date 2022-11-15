import { Component, OnInit, Input, ViewChild, ElementRef, AfterViewInit } from '@angular/core';

@Component({
  selector: 'mc-ring',
  templateUrl: './ring.component.html',
  styleUrls: ['./ring.component.scss']
})
export class RingComponent implements OnInit, AfterViewInit {

    private _text: string = 'This is a magic circle. It is very magical, but its also pretty weird.';

    @Input() width: string = '100%';
    @Input() height: string = '100%';
    @Input() color: string = '#fff';
    @Input() innerWidth: string = '60px';

    @ViewChild('textoutput') textEl!: ElementRef<HTMLDivElement>;

    @Input() set text(value: string) { this._text = value; this.doText(); }
    get text() { return this._text; }

    constructor() { }

    ngAfterViewInit(): void {
        this.doText();
    }

    ngOnInit(): void {
        this.doText();
    }

    private doText() {
        if (!this.textEl) return;

        const el = this.textEl.nativeElement;

        el.innerHTML = '';

        const letters = this.text.split('');
        if (letters.length == 0) return;

        letters.map((t, i) => {
            const e = document.createElement('div');
            e.classList.add('rotater');
            e.style.cssText = this.textStyle(i, letters.length);
            e.innerHTML = `<span>${t}</span>`;
            el.appendChild(e);
            return e;
        });
    }

    textStyle(i: number, size: number) {
        const rot = ((i + 1) / size) * 360;
        return `height: calc(${this.width});
        transform: rotate(${rot}deg);
        position: absolute;
        top: 0;
        left: 50%;
        transform-origin: center;`;
    }

    style() {
        return `width: ${this.width}; height: ${this.height}; border-color: ${this.color}; color: ${this.color}`;
    }

    innerStyle() {
        return `border-color: ${this.color}; color: ${this.color}; width: calc(100% - ${this.innerWidth}); height: calc(100% - ${this.innerWidth});`;
    }
}
