import { Component, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { ActivatedRoute } from '@angular/router';
import { PopupComponent, PopupInstance, PopupService } from 'src/app/components';
import { AuthService, LightNovelService, MangaFilter, MangaProgressData, MangaService, SubscriptionHandler } from 'src/app/services';

@Component({
    templateUrl: './manga-in-progress.component.html',
    styleUrls: ['./manga-in-progress.component.scss']
})
export class MangaInProgressComponent implements OnInit, OnDestroy {

    private _subs = new SubscriptionHandler();

    states = [
        { text: 'All', routes: ['/manga', 'filter', 'all'], index: 0 },
        { text: 'Completed', routes: ['/manga', 'filter', 'completed'], index: 2 },
        { text: 'In Progress', routes: ['/manga', 'filter', 'in-progress'], index: 3, aliases: ['inprogress'] },
        { text: 'Bookmarked', routes: ['/manga', 'filter', 'bookmarked'], index: 4 },
        { text: 'Favourites', routes: ['/manga', 'filter', 'favourite'], index: 1, aliases: [] },
        { text: 'Not Touched', routes: ['/manga', 'filter', 'not' ], index: 5, aliases: [] }
    ];

    @ViewChild('popup') popup!: PopupComponent;
    @ViewChild('mangaadd') mangaAdd!: PopupComponent;

    private _popIn?: PopupInstance;

    loading: boolean = false;
    error?: string;
    records: MangaProgressData[] = [];
    pages: number = 0;
    total: number = 0;
    state: number = 0;
    
    search!: MangaFilter;

    get current() { return this.states.find(t => t.index === this.state); }

    get params() { return this.api.routerParameters(this.search, this.state); }

    constructor(
        private api: MangaService,
        private auth: AuthService,
        private title: Title,
        private lnApi: LightNovelService,
        private route: ActivatedRoute,
        private pop: PopupService
    ) { }

    addManga() {
        this.pop.show(this.mangaAdd);
    }

    ngOnInit() {
        this.title.setTitle('CardboardBox | In Progress Manga');
        this._subs
            .subscribe(this.route.params, t => this.handleParams(t))
            .subscribe(this.auth.onLogin, () => this.handleParams())
            .subscribe(this.route.queryParams, () => this.handleParams());
    }

    ngOnDestroy() {
        this.title.setTitle(this.api.defaultTitle);
        this._subs.unsubscribe();
    }

    handleParams(map?: { [key: string]: any }) {
        this.state = this.determineType(map ? map['type'] : undefined);
        this.search = this.api.routeFilter(this.state);
        this.records = [];
        if (this.loading) return;

        this.loading = true;
        this.process();
    }

    proxy(url?: string) {
        if (!url) return '';
        return this.lnApi.corsFallback(url, 'manga-covers');
    }

    private determineType(type?: string) {
        type = (type || this.route.snapshot.paramMap.get('type')?.toString())?.toLocaleLowerCase()?.trim() || '';
        
        for(let item of this.states) {
            if (item.text.toLocaleLowerCase() === type) return item.index;

            const route = item.routes[2].toLocaleLowerCase();
            if (route === type) return item.index;

            const aliases = item.aliases || [];
            if (aliases.indexOf(type) !== -1) return item.index;
        }

        return 0;
    }

    private process() {
        this.search.state = this.state;
        this.api
            .search(this.search)
            .error(err => this.error = err.status, { pages: 0, count: 0, results: [] })
            .subscribe(t => {
                const { pages, count, results } = t;
                this.pages = pages;
                this.total = count;
                this.records = [...this.records, ...results];
                this.loading = false;
            });
    }

    onScroll() {
        this.search.page += 1;
        if (this.pages < this.search.page) return;
        this.process();
    }

    openFilters() { this._popIn = this.pop.show(this.popup); }
    closeFilters() { this._popIn?.ok(); }
    filter() { this.api.isSearching = true; }
}
