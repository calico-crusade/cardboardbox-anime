import { AfterViewInit, Directive, ElementRef, EventEmitter, OnDestroy, OnInit, Output } from "@angular/core";

@Directive({
    selector: '[observe]',
})
export class ObserveDirective implements OnDestroy, OnInit, AfterViewInit {
    
    @Output() visible = new EventEmitter<HTMLElement>();
    private observer: IntersectionObserver | undefined;

    constructor(private element: ElementRef) { }

    ngOnInit() {
        this.observer = new IntersectionObserver((e) => {
            e.forEach(entry => {
                if (!entry.isIntersecting) return;
                this.visible.next(entry.target as HTMLElement);
            });
        });
    }

    ngAfterViewInit() {
        if (!this.observer) return;
        this.observer.observe(this.element.nativeElement);
    }

    ngOnDestroy() {
        if (!this.observer) return;
        this.observer.disconnect();
        this.observer = undefined;
    }
}
