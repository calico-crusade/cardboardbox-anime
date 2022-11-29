import { Component, Input, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { PopupComponent, PopupInstance, PopupService } from 'src/app/components';
import { Filter, MangaFilter, MangaService, SubscriptionHandler } from 'src/app/services';

type Tag = {
    text: string;
    state: ('include' | 'exclude' | 'none');
}

@Component({
    selector: 'cba-manga-search-filters',
    templateUrl: './manga-search-filters.component.html',
    styleUrls: ['./manga-search-filters.component.scss']
})
export class MangaSearchFiltersComponent implements OnInit, OnDestroy {

    private _subs = new SubscriptionHandler();

    @ViewChild('searchpopup') searchPop!: PopupComponent;
    @Input('override-state') overrideState: number = -1;
    @Input('route') routeParts: string[] = ['/manga', 'all'];

    states: string[] = [
        'All Manga', //0 - All
        'Your Favourites', //1 - Favourite 
        'Read', //2 - Completed
        'Reading', //3 - In Progress
        'Has Bookmarks', //4 - Bookmarked
        'Not Touched', //5 - Else
    ];
    
    filters: Filter[] = [];
    filterInstance?: PopupInstance;
    allTags: string[] = [];
    allSorts: string[] = [];

    tags: Tag[] = [];

    search: MangaFilter = {
        page: 1,
        size: 20,
        search: '',
        include: [],
        exclude: [],
        asc: true,
        sort: 0,
        state: 0
    };

    constructor(
        private api: MangaService,
        private pop: PopupService,
        private router: Router,
        private route: ActivatedRoute
    ) { }

    ngOnInit(): void {
        this.api.filters().subscribe(t => {
            this.filters = t;
            this.allTags = this.filters.find(t => t.key === 'tag')?.values || [];
            this.allSorts = this.filters.find(t => t.key === 'sorts')?.values || [];
            this.setTags();
        });
        this._subs
            .subscribe(this.api.onSearch, t => {
                if (!t) {
                    this.filterInstance?.cancel();
                    return;
                }
    
                if (this.searchPop) this.filterInstance = this.pop.show(this.searchPop);
            })
            .subscribe(this.route.queryParams, t => {
                const state = this.overrideState < 0 ? undefined : this.overrideState;
                this.search = this.api.routeFilter(state);
                this.setTags();
            });
    }

    ngOnDestroy(): void { this._subs.unsubscribe(); }

    toggleFilter(tag: Tag) {
        if (tag.state === 'include') {
            tag.state = 'exclude';
            return;
        }

        if (tag.state === 'exclude') {
            tag.state = 'none';
            return;
        }

        tag.state = 'include';
    }

    getState(tag: string) {
        if (this.search.include.indexOf(tag) !== -1) return 'include';
        if (this.search.exclude.indexOf(tag) !== -1) return 'exclude';
        return 'none';
    }

    doSearch() {
        let pars: { [key: string]: any } = {};

        let { includes, excludes } = this.reverseTags();

        if (this.search.search) pars['search'] = this.search.search;
        if (includes.length > 0) pars['include'] = includes.join(',');
        if (excludes.length > 0) pars['exclude'] = excludes.join(',');
        if (!this.search.asc) pars['desc'] = true;
        if (this.search.sort) pars['sort'] = this.search.sort;
        if (this.search.state && this.overrideState < 0) pars['state'] = this.search.state;
        if (this.search.nsfw) pars['nsfw'] = this.search.nsfw;

        this.search.page = 1;
        this.router.navigate(this.routeParts, { queryParams: pars });
        this.filterInstance?.cancel();
    }

    reverseTags() {
        const includes: string[] = [];
        const excludes: string[] = [];

        for(let tag of this.tags) {
            if (tag.state === 'include') includes.push(tag.text);
            if (tag.state === 'exclude') excludes.push(tag.text);
        }

        return { includes, excludes };
    }

    setTags() {
        this.tags = this.allTags.map(t => {

            const isInclude = this.search.include.indexOf(t) !== -1;
            const isExclude = this.search.exclude.indexOf(t) !== -1;

            return {
                text: t,
                state: isInclude ? 'include': (isExclude ? 'exclude' : 'none')
            }
        });
    }
}
