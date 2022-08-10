import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { 
    AnimeService, AuthService, UtilitiesService,
    Anime, FilterSearch, MatureType, AuthUser, ListsMaps
} from './../../services';

const STORE_TUT = 'showTut';

type VrvAnimeImage = Anime & { src: string; };

@Component({
    templateUrl: './anime.component.html',
    styleUrls: ['./anime.component.scss']
})
export class AnimeComponent implements OnInit, OnDestroy {

    defaultImage = '/assets/default-background.webp';

    bgs: VrvAnimeImage[] = [];
    curIndex: number = 0;
    interval: any;
    filtersOpen: boolean = false;
    anime: Anime[] = [];
    pages: number = 0;
    total: number = 0;
    showTut: boolean = true;
    loading: boolean = true;
    curUser?: AuthUser;
    filter?: FilterSearch;
    listId?: number;
    map?: ListsMaps;

    get current() {
        if (this.bgs.length === 0) return undefined;
        return this.bgs[this.curIndex];
    }

    get list() {
        if (!this.map || !this.listId) return undefined;
        return this.map.lists[this.listId];
    }

    constructor(
        private api: AnimeService,
        private util: UtilitiesService,
        private router: Router,
        private auth: AuthService,
        private route: ActivatedRoute
    ) { }

    ngOnInit(): void {
        this.showTut = !localStorage.getItem(STORE_TUT);
        this.auth.onLogin.subscribe(t => {
            this.curUser = t;
            if (!this.curUser) return;
            this.api.map.subscribe(t => this.map = t);
        });
        this.route.params.subscribe(t => {
            this.listId = t['id'];
        });
    }

    process() {
        this.loading = true;
        if (!this.filter) return;

        if (this.filter.page === 1) {
            this.loading = true;
            this.anime = [];
        }

        this.api
            .search(this.filter)
            .subscribe(t => {
                this.loading = false;
                if (this.filter?.page === 1)
                    this.anime = [];

                if (t.results.length === 0) return;

                if (this.anime.length > 0) {
                    const last = this.anime[this.anime.length - 1];
                    const first = t.results[0];

                    if (last.hashId === first.hashId) {
                        t.results.splice(0, 1);
                    }
                }

                this.anime.push(...t.results);
                this.pages = t.pages;
                this.total = t.count;

                if (!this.interval) this.handleBgs();
            });
    }

    moveNext() {
        if (!this.filter) return;

        this.filter.page++;
        this.process();
    }

    updateRoute() {
        if (!this.filter) return;
        
        let params: { [key: string]: any } = { };

        if (this.filter.mature !== MatureType.Both) params['mature'] = +this.filter.mature;
        if (!this.filter.asc) params['asc'] = 'false';
        if (this.filter.search) params['search'] = this.filter.search;
        
        for(const key in this.filter.queryables) {
            const vals = this.filter.queryables[key];
            params[key] = vals.join(',');
        }

        const parts = ['/anime'];
        if (this.listId) parts.push(this.listId.toString());

        this.router.navigate(parts, {
            queryParams: params
        });
    }

    randomImage() {
        if (!this.anime || this.anime.length <= 0) return undefined;

        let anime = this.util.rand(this.anime);
        let src = `url("${this.getMaxImage(anime).source}")`;

        return <VrvAnimeImage>{
            ...anime,
            src
        };
    }

    handleBgs() {
        this.bgs = [];
        for(let i = 0; i < 3; i++) {
            let im = this.randomImage();
            if (!im) continue;
            this.bgs.push(im);
        }

        this.interval = setInterval(() => {
            let p = this.util.indexInBounds(this.bgs, this.curIndex - 1);
            let n = this.util.indexInBounds(this.bgs, this.curIndex + 1);
            this.curIndex = n;
            let im = this.randomImage();
            if (!im) return;
            this.bgs[p] = im;
        }, 4000);
    }

    ngOnDestroy(): void {
        clearInterval(this.interval);
    }

    getMaxImage(anime: Anime) {
        return anime?.images
            ?.filter(t => t.type === 'wallpaper')
            ?.reduce((p, v) => p.height && p.width && v.height && v.width && p.height > v.height && p.width > v.width ? p : v);
    }

    toggleTut() {
        this.showTut = false;
        localStorage.setItem(STORE_TUT, 'true');
    }

    onScroll() {
        this.moveNext();
    }

    onSearch(filter: FilterSearch) {
        this.filtersOpen = false;
        this.filter = filter;
        this.filter.listId = this.listId;
        this.process();
        this.updateRoute();
    }
}
