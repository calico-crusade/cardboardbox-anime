import { Directive, HostBinding, Input, HostListener } from "@angular/core";
import { LightNovelService } from "../services";

@Directive({
    selector: 'img[fallback]'
})
export class ImageFallbackDirective {

    private _src?: string;
    private _fallback?: string;
    private _defaultDefault: string = 'https://mangabox.app/broken.png';

    @HostBinding('src') imageSrc?: string;

    @Input()
    set src(value: string | undefined) {
        this._src = value;
        this.imageSrc = value;
    }
    get src() { return this._src; }

    @Input('fallback')
    set default(value: string | undefined) { this._fallback = value; }
    get default() { return this._fallback || this._defaultDefault; }

    constructor(
        private api: LightNovelService
    ) { }

    @HostListener('error')
    update() {
        if (this.imageSrc === this.default) return;

        if (!this.imageSrc) {
            this.imageSrc = this.default;
            return;
        }


        if (this.imageSrc === this.src) {
            this.imageSrc = this.api.corsFallback(this.src, 'image-fallback');
            return;
        }

        this.imageSrc = this.default;
    }

}
