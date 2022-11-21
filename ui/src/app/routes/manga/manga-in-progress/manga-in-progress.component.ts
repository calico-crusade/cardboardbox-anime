import { Component, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { ActivatedRoute } from '@angular/router';
import { PopupComponent, PopupInstance, PopupService } from 'src/app/components';
import { AuthService, LightNovelService, MangaProgressData, MangaService } from 'src/app/services';

@Component({
    templateUrl: './manga-in-progress.component.html',
    styleUrls: ['./manga-in-progress.component.scss']
})
export class MangaInProgressComponent implements OnInit, OnDestroy {

    @ViewChild('popup') popup!: PopupComponent;
    private _popIn?: PopupInstance;

    loading: boolean = false;
    error?: string;

    records: MangaProgressData[] = [];
    page: number = 1;
    size: number = 20;
    pages: number = 0;
    total: number = 0;
    type?: string;

    get properType() {
        const type = this.type?.toLocaleLowerCase();
        switch(type) {
            case 'favourite': return 'favourite';
            case 'completed': return 'completed';
            case 'inprogress':
            case 'in-progress': return 'inprogress';
            case 'bookmarked': return 'bookmarked';
            default: return undefined;
        }
    }

    constructor(
        private api: MangaService,
        private auth: AuthService,
        private title: Title,
        private lnApi: LightNovelService,
        private route: ActivatedRoute,
        private pop: PopupService
    ) { }

    ngOnInit() {
        this.title.setTitle('CardboardBox | In Progress Manga');
        this.route.params.subscribe(t => this.handleParams(t));
        this.auth.onLogin.subscribe(t => this.handleParams());
    }

    ngOnDestroy() {
        this.title.setTitle(this.api.defaultTitle);
    }

    handleParams(map?: { [key: string]: any }) {
        this.type = map ? map['type'] : this.route.snapshot.paramMap.get('type')?.toString();
        this.page = 1;
        this.records = [];
        this.loading = true;
        this.process();
    }

    proxy(url?: string) {
        if (!url) return '';
        return this.lnApi.corsFallback(url, 'manga-covers');
    }

    private process() {
        if (!this.auth.currentUser) return;

        this.api
            .touched(this.page, this.size, this.properType)
            .error(err => this.error = err.status, { pages: 0, count: 0, results: [] })
            .subscribe(t => {
                const { pages, count, results } = t;
                this.pages = pages;
                this.total = count;
                this.records = [ ...this.records, ...results ];
                this.loading = false;
            });
    }

    onScroll() {
        this.page += 1;
        if (this.pages < this.page) return;

        this.process();
    }

    openFilters() {
        this._popIn = this.pop.show(this.popup);
    }

    closeFilters() {
        this._popIn?.ok();
    }
}
