import { Directive, ElementRef, HostListener, Input, OnDestroy } from "@angular/core";

@Directive({
    selector: '[cba-tooltip]'
})
export class TooltipDirective implements OnDestroy {

    @Input('cba-tooltip') tooltip!: string;
    @Input('delay') delay: number = 50;
    @Input('direction') direction: ('left' | 'right' | 'up' | 'down') = 'left';

    private popup?: any;
    private timer?: any;

    constructor(
        private ref: ElementRef
    ) { }

    ngOnDestroy(): void {
        this.destroy();
    }

    @HostListener('mouseenter') 
    mouseEnter() {
        if (this.popup) return;

        this.timer = setTimeout(() => {
            const rect = this.ref.nativeElement.getBoundingClientRect();
            this.createPopup(rect);

            setTimeout(() => { this.destroy(); }, 1000 * 10)
        }, this.delay);
    }

    @HostListener('mouseleave') 
    mouseLeave() { this.destroy(); }

    private createPopup(rect: DOMRect) {
        let popup = document.createElement('div');
        popup.innerHTML = this.tooltip;
        popup.setAttribute('class', 'tooltip-container');
        document.body.appendChild(popup);

        const { x, y } = this.determinePosition(rect, popup);
        
        popup.style.top = y + 'px';
        popup.style.left = x + 'px';
        this.popup = popup;
    }

    private determinePosition(rect: DOMRect, pop: HTMLDivElement) {
        let x, y;
        switch(this.direction) {
            case 'up':
                y = rect.top - pop.clientHeight - 5;
                x = rect.left + ((rect.width / 2) - (pop.clientWidth / 2));
                return { x, y };
            case 'down':
                y = rect.top + pop.clientHeight + 5;
                x = rect.left + ((rect.width / 2) - (pop.clientWidth / 2));
                return { x, y };
            case 'right':
                y = rect.top + (rect.height / 2) - (pop.clientHeight / 2);
                x = rect.left + rect.width + 5;
                return { x, y };
            default:
                y = rect.top + (rect.height / 2) - (pop.clientHeight / 2);
                x = rect.left - pop.clientWidth - 5;
                return { x, y };
        }
    }

    private destroy() {
        if (this.popup) {
            this.popup.remove();
            this.popup = undefined;
        }

        if (this.timer) {
            clearTimeout(this.timer);
            this.timer = undefined;
        }
    }
}