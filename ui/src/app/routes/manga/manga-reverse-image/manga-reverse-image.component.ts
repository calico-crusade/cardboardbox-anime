import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ImageSearch, MangaService, SubscriptionHandler } from './../../../services';

@Component({
    templateUrl: './manga-reverse-image.component.html',
    styleUrls: ['./manga-reverse-image.component.scss']
})
export class MangaReverseImageComponent implements OnInit, OnDestroy {

    private _subs = new SubscriptionHandler();

    loading: boolean = false;
    url: string = '';
    filename?: string;
    results?: ImageSearch;
    error?: any;

    get combined() {
        if (!this.results) return [];

        return [
            ...this.results.match,
            ...this.results.vision,
            ...this.results.textual
        ];
    }

    get best() {
        return this.combined.find(t => t.manga.id === this.results?.bestGuess?.id) || this.results?.bestGuess;
    }

    constructor(
        private api: MangaService,
        private route: ActivatedRoute,
        private router: Router
    ) { }

    ngOnInit(): void {
        this._subs
            .subscribe(this.route.queryParams, (t) => {
                this.url = t['search'] || '';
                this.textSearch();
            });
    }

    ngOnDestroy() { this._subs.unsubscribe(); }

    selected(event: Event) {
        this.url = '';
        if (!event || !event.target) return;
        const files: File[] = (<any>event.target).files;
        if (!files || files.length <= 0) return;

        this.loading = true;
        const file = files[0];

        this.api
            .imageSearch(file)
            .error(t => {
                this.error = t;
            }, { vision: [], match: [], textual: [] })
            .subscribe(t => {
                this.results = t;
                this.loading = false;
            });
    }

    search() {
        this.router.navigate(['/manga', 'search'], { queryParams: { search: this.url } });
    }

    private textSearch() {
        if (!this.url) return;

        this.loading = true;
        this.api
            .imageSearch(this.url)
            .error(t => {
                this.error = t;
            }, { vision: [], match: [], textual: [] })
            .subscribe(t => {
                this.results = t;
                this.loading = false;
            });
    }
}
