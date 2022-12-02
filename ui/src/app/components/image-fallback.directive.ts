import { Directive, HostBinding, Input, HostListener } from "@angular/core";
import { LightNovelService } from "../services";

@Directive({
    selector: 'img[fallback]'
})
export class ImageFallbackDirective {

    @HostBinding('src') @Input() src?: string;

    @Input('fallback') default?: string;

    constructor(
        private api: LightNovelService
    ) { }

    @HostListener('error')
    update() {
        this.src = this.default || this.api.corsFallback(this.src || '', 'image-fallback');
    }

}