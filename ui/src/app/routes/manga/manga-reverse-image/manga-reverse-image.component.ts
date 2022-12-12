import { Component } from '@angular/core';
import { ImageSearch, LightNovelService, MangaService } from 'src/app/services';

@Component({
    templateUrl: './manga-reverse-image.component.html',
    styleUrls: ['./manga-reverse-image.component.scss']
})
export class MangaReverseImageComponent {

    loading: boolean = false;
    url: string = '';
    filename?: string;
    results?: ImageSearch;
    error?: any;

    constructor(
        private api: MangaService,
        private ln: LightNovelService
    ) { }

    selected(event: Event) {
        if (!event || !event.target) return;
        const files: File[] = (<any>event.target).files;
        if (!files || files.length <= 0) return;

        this.loading = true;
        const file = files[0];

        this.api
            .imageSearch(file)
            .error(t => {
                this.error = t;
            }, { vision: [], match: [] })
            .subscribe(t => {
                this.results = t;
                this.loading = false;
            });
    }

    search() {
        if (!this.url) return;

        this.loading = true;
        this.api
            .imageSearch(this.url)
            .error(t => {
                this.error = t;
            }, { vision: [], match: [] })
            .subscribe(t => {
                this.results = t;
                this.loading = false;
            });
    }

    proxy(url: string) { return this.ln.corsFallback(url, 'image-fallback'); }

    domain(url: string) { return new URL(url).hostname; }
}
