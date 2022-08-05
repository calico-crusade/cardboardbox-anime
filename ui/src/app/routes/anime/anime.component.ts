import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Anime, FilterSearch } from 'src/app/services/anime.model';
import { AnimeService } from 'src/app/services/anime.service';
import { UtilitiesService } from 'src/app/services/utilities.service';

const STORE_TUT = 'showTut';

type VrvAnimeImage = Anime & { src: string; };

@Component({
    templateUrl: './anime.component.html',
    styleUrls: ['./anime.component.scss']
})
export class AnimeComponent implements OnInit, OnDestroy {

    bgs: VrvAnimeImage[] = [];
    curIndex: number = 0;
    interval: any;
    filtersOpen: boolean = false;
    anime: Anime[] = [];
    pages: number = 0;
    total: number = 0;
    showTut: boolean = true;
    loading: boolean = true;

    filter?: FilterSearch;

    get current() {
        if (this.bgs.length === 0) return undefined;

        return this.bgs[this.curIndex];
    }

    constructor(
        private api: AnimeService,
        private util: UtilitiesService,
        private router: Router
    ) { }

    ngOnInit(): void {
        this.showTut = !localStorage.getItem(STORE_TUT);
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
                if (this.filter?.page === 1)
                    this.anime = [];

                this.anime.push(...t.results);
                this.pages = t.pages;
                this.total = t.count;
                this.loading = false;

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

        if (this.filter.mature !== undefined) params['mature'] = +this.filter.mature;
        if (!this.filter.asc) params['asc'] = 'false';
        if (this.filter.search) params['search'] = this.filter.search;
        
        for(const key in this.filter.queryables) {
            const vals = this.filter.queryables[key];
            params[key] = vals.join(',');
        }

        this.router.navigate([ '/anime' ], {
            queryParams: params
        });
    }

    randomImage() {
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

    toggleTut() {
        this.showTut = false;
        localStorage.setItem(STORE_TUT, 'true');
    }

    onScroll() {
        this.moveNext();
    }

    onSearch(filter: FilterSearch) {
        this.filter = filter;
        this.process();
        this.updateRoute();
    }
}
