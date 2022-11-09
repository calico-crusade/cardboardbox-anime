import { Directive, HostBinding, Input } from "@angular/core";
import { LightNovelService } from "../services";

@Directive({
    selector: 'img[proxy]'
})
export class ImageProxyDirective {

    @HostBinding('src') @Input() src?: string;

    @Input() group: string = 'anime';

    @Input() 
    set proxy(value: string) {
        this.src = this.api.corsFallback(value, this.group);
    }

    constructor(
        private api: LightNovelService
    ) { }
}