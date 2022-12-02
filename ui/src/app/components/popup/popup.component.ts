import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';

@Component({
    selector: 'cba-popup',
    templateUrl: './popup.component.html',
    styleUrls: ['./popup.component.scss']
})
export class PopupComponent {

    show: boolean = false;

    @Input('background') offClick: boolean = true;
    @Input('size') size: ('big' | 'normal' | 'medium') = 'normal';
    @Input('color') color: string = 'var(--color-vrv)';
    @Input('remove-close') removeClose: boolean = false;

    @Output() canceled: EventEmitter<void> = new EventEmitter();
    @Output() confirmed: EventEmitter<void> = new EventEmitter();

    constructor() { }

    cancel(isBackground: boolean = false) {
        if (isBackground && !this.offClick) return;

        this.canceled.emit();
        this.show = false;
    }

    ok() {
        this.confirmed.emit();
        this.show = false;
    }

}
