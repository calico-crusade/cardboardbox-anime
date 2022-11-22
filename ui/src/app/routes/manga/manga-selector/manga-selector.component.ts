import { Component, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { ActivatedRoute } from '@angular/router';
import { PopupComponent, PopupService } from 'src/app/components';
import { LightNovelService, MangaFilter, MangaProgressData, MangaService } from 'src/app/services';

@Component({
    templateUrl: './manga-selector.component.html',
    styleUrls: ['./manga-selector.component.scss']
})
export class MangaSelectorComponent implements OnInit, OnDestroy {

    loading: boolean = false;
    data: MangaProgressData[] = [];
    pages: number = 0;
    url: string = '';
    search!: MangaFilter;

    @ViewChild('popup') popup!: PopupComponent;

    constructor(
        private api: MangaService,
        private pop: PopupService,
        private route: ActivatedRoute,
        private lnApi: LightNovelService,
        private title: Title
    ) { }

    proxy(url?: string) {
        if (!url) return '';
        return this.lnApi.corsFallback(url, 'manga-covers');
    }

    ngOnInit() {
        this.title.setTitle('CardboardBox | Manga');
        this.route.queryParams.subscribe(t => {
            this.search = this.api.routeFilter();
            this.data = [];
            this.process();
        });
    }

    ngOnDestroy(): void {
        this.title.setTitle(this.api.defaultTitle);
    }

    process() {
        this.api
            .search(this.search)
            .subscribe(t => {
                const { results, pages } = t;
                this.pages = pages;
                this.data = [ ...this.data, ...results ];
            });
    }

    onScroll() {
        this.search.page += 1;
        if (this.pages < this.search.page) return;

        this.process();
    }

    add() { this.pop.show(this.popup); }

    filter() { this.api.isSearching = true; }
}
