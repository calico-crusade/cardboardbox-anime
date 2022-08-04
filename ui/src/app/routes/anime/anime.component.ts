import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Anime } from 'src/app/services/anime.model';
import { AnimeService } from 'src/app/services/anime.service';
import { UtilitiesService } from 'src/app/services/utilities.service';

const STORE_TUT = 'showTut';

type VrvAnimeImage = Anime & { src: string; };
type CheckOption = {
    text: string;
    checked: boolean;
    actual?: string;
};
type CheckOptions = CheckOption[];
type AvailableParams = 'channels' | 'languages' | 'ratings' | 'types' | 'tags';
type QueryParam = {
    param: AvailableParams;
    field: (anime: Anime) => string[];
    options: CheckOptions;
    show: boolean;
    enabled: boolean;
    defaults: string[];
};
type QueryParams = QueryParam[];

@Component({
    templateUrl: './anime.component.html',
    styleUrls: ['./anime.component.scss']
})
export class AnimeComponent implements OnInit, OnDestroy {

    bgs: VrvAnimeImage[] = [];
    curIndex: number = 0;
    interval: any;
    filtersOpen: boolean = false;
    filters: QueryParams = this.generateFilters();
    search: string = '';
    filteredTotal: number = 0;
    anime: Anime[] = [];
    showTut: boolean = true;

    private _anime: Anime[] = [];
    private _page: number = 1;
    private _size: number = 50;
    private _totalPages: number = 0;
    private _totalCount: number = 0;

    get total() { return this._anime.length; }

    get current() {
        if (this.bgs.length === 0) return undefined;

        return this.bgs[this.curIndex];
    }

    constructor(
        private api: AnimeService,
        private util: UtilitiesService,
        private route: ActivatedRoute,
        private router: Router
    ) { }

    onlyValid(opts: CheckOptions) {
        return opts
            .filter(t => t.checked)
            .map(t => !!t.actual ? t.actual : t.text);
    }

    ngOnInit(): void {
        this.showTut = !localStorage.getItem(STORE_TUT);

        this.api
            .page(this._page, this._size, true)
            .subscribe(t => {
                this._anime = t.results;
                this._totalPages = t.pages;
                this._totalCount = t.count;
                
                this.getFilters();
                this.handleBgs();
                this.filter();
            });

        this.route.queryParams.subscribe(t => {
            for(let filter of this.filters) {
                const fs = (t[filter.param] || '') + '';
                if (!fs) continue;

                const opts = fs.split(',').map(a => a.trim());
                if (opts.length === 0) continue;
                filter.defaults = opts;

                for(let opt of filter.options) {
                    opt.checked = this.util.any(opts, opt.actual || opt.text);
                }
            }

            this.filter();
        });
    }

    searchFilter() {
        this.filter();
    }

    filter() {
        const con: { [key: string]: string[] } = {};
        const search = this.search.length >= 3 ? this.search.toLowerCase() : '';

        for(let filter of this.filters) {
            con[filter.param] = this.onlyValid(filter.options);
        }

        let filtered = this._anime.filter(t => {
            if (search && t.title.toLowerCase().indexOf(search) === -1) {
                return false;
            }

            for(let filter of this.filters) {
                if (!filter.enabled) continue;

                if (!this.util.anyOf(con[filter.param], filter.field(t))) return false;
            }

            return true;
        });

        this.anime = filtered;
    }

    updateRoute() {
        let params: { [key: string]: any } = {};

        for(let filter of this.filters) {
            if (this.util.all(filter.options, t => t.checked)) continue;
            params[filter.param] = this.onlyValid(filter.options).join(',');
        }

        this.router.navigate([ '/anime' ], {
            queryParams: params
        });
    }

    toggle(opt: CheckOption) {
        opt.checked = !opt.checked;
        this.updateRoute();
    }

    randomImage() {
        let anime = this.util.rand(this._anime);
        let src = `url("${this.getMaxImage(anime).source}")`;

        return <VrvAnimeImage>{
            ...anime,
            src
        };
    }

    handleBgs() {
        this.bgs = [];
        for(let i = 0; i < 3; i++) {
            this.bgs.push(this.randomImage());
        }

        this.interval = setInterval(() => {
            let p = this.util.indexInBounds(this.bgs, this.curIndex - 1);
            let n = this.util.indexInBounds(this.bgs, this.curIndex + 1);
            this.curIndex = n;
            this.bgs[p] = this.randomImage();
        }, 4000);
    }

    ngOnDestroy(): void {
        clearInterval(this.interval);
    }

    getMaxImage(anime: Anime) {
        return anime.images
            .filter(t => t.type === 'wallpaper')
            .reduce((p, v) => p.height && p.width && v.height && v.width && p.height > v.height && p.width > v.width ? p : v);
    }

    getFilters() {
        const channels: string[] = [];
        const tags: string[] = [];
        const ratings: string[] = [];

        for(const anime of this._anime) {
            if (channels.indexOf(anime.platformId) === -1)
                channels.push(anime.platformId);
            
            for(const tag of anime.metadata?.tags || []) {
                if (tags.indexOf(tag) === -1)
                    tags.push(tag);
            }

            for(const rating of anime.metadata.ratings) {
                if (ratings.indexOf(rating) === -1)
                    ratings.push(rating);
            }
        }

        const cF = this.filters.find(t => t.param === 'channels');
        const tF = this.filters.find(t => t.param === 'tags');
        const rF = this.filters.find(t => t.param === 'ratings');

        const toChecked = (t: string[]) => {
            return t.map(a => {
                return {
                    text: a,
                    checked: true
                }
            });
        }

        if (cF) cF.options = toChecked(channels);
        if (tF) tF.options = toChecked(tags);
        if (rF) rF.options = toChecked(ratings);
    }

    generateFilters(): QueryParams {
        return [
            {
                param: 'channels',
                field: (t) => [ t.platformId ],
                options: [],
                show: true,
                defaults: [],
                enabled: true
            }, {
                field: (t) => t.metadata.languageTypes,
                options: [
                    { text: 'Subbed', checked: true },
                    { text: 'Dubbed', checked: true }, 
                    { text: 'Unknown', checked: true }
                ],
                show: true,
                defaults: [],
                enabled: true,
                param: 'languages'
            }, {
                field: (t) => [ t.type ],
                options: [
                    { text: 'Series', checked: true, actual: 'series' },
                    { text: 'Movie', checked: true, actual: 'movie_listing' }
                ],
                show: true,
                defaults: [],
                enabled: true,
                param: 'types'
            }, {
                field: (t) => t.metadata.ratings,
                options: [],
                show: true,
                defaults: [],
                enabled: true,
                param: 'ratings'
            }, {
                field: (t) => t.metadata.tags,
                options: [],
                show: false,
                defaults: [],
                enabled: false,
                param: 'tags'
            }
        ]
    }

    allChecked(filter: QueryParam) {
        return this.util.all(filter.options, t => t.checked);
    }
    
    toggleAll(filter: QueryParam) {
        const val = !this.allChecked(filter);
        for(let f of filter.options)
            f.checked = val;
        this.updateRoute();
    }

    toggleTut() {
        this.showTut = false;
        localStorage.setItem(STORE_TUT, 'true');
    }
}
