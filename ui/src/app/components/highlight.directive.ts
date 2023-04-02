import { Directive, ElementRef, EventEmitter, HostListener, Input, Output } from "@angular/core";

export interface Selection {
    position: DOMRect;
    text: string;
}

@Directive({
    selector: '[text-highlighted]'
})
export class HighlightDirective {

    current?: Selection;

    @Input('deny-exits') denyExits?: string | boolean;
    @Output('text-highlighted') output = new EventEmitter<Selection | undefined>();

    constructor(
        private el: ElementRef
    ) { }

    @HostListener('mouseup')
    event() {
        const data = document.getSelection();
        const text = data?.toString();
        if (this.current && (!data || !text)) {
            this.current = undefined;
            this.output.emit(undefined);
            return;
        }

        if (!data || !text) return;

        const position = data.getRangeAt(0).getBoundingClientRect();
        const results = { text, position }
        this.current = results;
        this.output.emit(results);
    }

    @HostListener('mouseleave', ['$event'])
    leave(event: MouseEvent) {
        if (!this.current || !(this.denyExits !== '' || !this.denyExits)) return;

        const box = this.el.nativeElement.getBoundingClientRect();

        if (this.in(box, event.clientX, event.clientY)) return;

        this.current = undefined;
        this.output.emit(undefined);
    }

    in(parent: DOMRect, x: number, y: number) {
        return x > parent.x && x < parent.x + parent.width &&
            y > parent.y && y < parent.y + parent.height;
    }
}