import { Component, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { ActivatedRoute, Router } from '@angular/router';
import { catchError, of } from 'rxjs';
import { PopupComponent, PopupInstance, PopupService } from 'src/app/components';
import { Filter, LightNovelService, Manga, MangaFilter, MangaService } from 'src/app/services';

type SearchTag = {
    value: string;
    state: 'none' | 'include' | 'exclude'
};

@Component({
    templateUrl: './manga-selector.component.html',
    styleUrls: ['./manga-selector.component.scss']
})
export class MangaSelectorComponent implements OnInit, OnDestroy {

    loading: boolean = false;

    data: Manga[] = [];
    filters: Filter[] = [];
    filterInstance?: PopupInstance;
    
    pages: number = 0;

    url: string = '';

    @ViewChild('popup') popup!: PopupComponent;
    @ViewChild('searchpopup') searchPop!: PopupComponent;

    allTags: string[] = [];

    search: MangaFilter = {
        page: 1,
        size: 20,
        search: '',
        include: [],
        exclude: [],
        asc: true
    };

    constructor(
        private api: MangaService,
        private pop: PopupService,
        private router: Router,
        private route: ActivatedRoute,
        private lnApi: LightNovelService,
        private title: Title
    ) { }

    proxy(url?: string) {
        if (!url) return '';
        return this.lnApi.corsFallback(url);
    }

    ngOnInit() {
        this.api.filters().subscribe(t => {
            this.filters = t;
            this.allTags = this.filters.find(t => t.key === 'tag')?.values || [];
        });
        this.title.setTitle('CardboardBox | Manga');
        this.route.queryParams.subscribe(t => {
            if (t['search']) this.search.search = t['search'];
            if (t['desc']) this.search.asc = false;
            if (t['include']) this.search.include = t['include'].split(',');
            if (t['exclude']) this.search.exclude = t['exclude'].split(',');
            this.process();
        });
    }

    ngOnDestroy(): void {
        this.title.setTitle(this.api.defaultTitle);
    }

    doSearch() {
        let pars: { [key: string]: any } = {};

        if (this.search.search) pars['search'] = this.search.search;
        if (this.search.include.length > 0) pars['include'] = this.search.include.join(',');
        if (this.search.exclude.length > 0) pars['exclude'] = this.search.exclude.join(',');
        if (!this.search.asc) pars['desc'] = true;

        this.search.page = 1;
        this.router.navigate(['/manga'], { queryParams: pars });
        this.filterInstance?.cancel();
    }

    process() {
        this.api
            .search(this.search)
            .subscribe(t => {
                if (this.search.page === 1)
                    this.data = [];

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

    load() {
        this.loading = true;
        this.api
            .manga(this.url)
            .pipe(
                catchError(err => {
                    console.error('Error occurred while loading manga', {
                        url: this.url,
                        err
                    });
                    alert('An error occurred while trying to load your manga!');
                    return of(undefined);
                })
            )
            .subscribe(t => {
                this.loading = false;
                if (!t) return;

                this.router.navigate(['/manga', t.manga.id]);
            });
    }

    filter() { this.filterInstance = this.pop.show(this.searchPop); }

    toggleFilter(tag: string) {
        const ii = this.search.include.indexOf(tag);
        const ei = this.search.exclude.indexOf(tag);

        if (ii === -1 && ei === -1) {
            this.search.include.push(tag);
            return;
        }

        if (ii !== -1 && ei !== -1) {
            //This is technically, an error state?
            this.search.include.splice(ii);
            this.search.exclude.splice(ei);
            return;
        }

        if (ii === -1 && ei !== -1) {
            this.search.exclude.splice(ei);
            return;
        } 

        if (ii !== -1 && ei === -1) {
            this.search.include.splice(ii);
            this.search.exclude.push(tag);
            return;
        }
    }

    getState(tag: string) {
        if (this.search.include.indexOf(tag) !== -1) return 'include';
        if (this.search.exclude.indexOf(tag) !== -1) return 'exclude';
        return 'none';
    }
}
