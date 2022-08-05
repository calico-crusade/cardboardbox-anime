import { Component, OnInit, Output, EventEmitter } from '@angular/core';
import { ActivatedRoute, Params } from '@angular/router';
import { AvailableParams, Filter, Filters, FilterSearch, MatureType } from 'src/app/services/anime.model';
import { AnimeService } from 'src/app/services/anime.service';
import { UtilitiesService } from 'src/app/services/utilities.service';

@Component({
    selector: 'cba-search-filters',
    templateUrl: './search-filters.component.html',
    styleUrls: ['./search-filters.component.scss']
})
export class SearchFiltersComponent implements OnInit {

    private _loading: boolean = false;

    filter?: FilterSearch;
    available?: Filters;
    currentParams?: Params;

    @Output('searched') onSearch: EventEmitter<FilterSearch> = new EventEmitter();

    get loading() {
        if (this._loading) return true;

        if (!this.filter) return true;
        if (!this.available) return true;

        return false;
    }

    constructor(
        private api: AnimeService,
        private route: ActivatedRoute,
        private util: UtilitiesService
    ) { }

    ngOnInit(): void {
        this.api
            .filters()
            .subscribe(t => {
                this.available = t;
                this.process();
            });

        this.route.queryParams.subscribe(t => {
            this.currentParams = t;
            this.process();
        });
    }

    toggleMature(value: MatureType) {
        if (!this.filter) return;
        this.filter.mature = value;
    }

    checked(filter: Filter, opt: string) {
        if (!this.filter) return false;

        const cur = this.filter.queryables[filter.key];
        return cur.indexOf(opt) !== -1;
    }

    toggle(filter: Filter, opt: string) {
        if (!this.filter) return;

        const cur = this.filter.queryables[filter.key];
        const i = cur.indexOf(opt);

        if (i === -1) {
            cur.push(opt);
            return;
        }

        cur.splice(i, 1);
    }

    process() {
        if (!this.available) return;
        let p = <any>this.currentParams;

        const filter: FilterSearch = {
            page: 1,
            size: 50,
            asc: p.asc === undefined ? true : false,
            mature: p.mature,
            search: p.search || '',
            queryables: { }
        };

        const getParamVal = (key: AvailableParams, vals: string[]): string[] => {
            let val: string = p[key];
            if (!val) { return JSON.parse(JSON.stringify(vals)); }

            return val.split(',');
        };

        for(let a of this.available) {
            const vals = getParamVal(a.key, a.values);
            filter.queryables[a.key] = vals;
        }

        this.filter = filter;
        this.search();
    }

    search() {
        if (!this.filter || !this.available) return;

        const copy = <FilterSearch>JSON.parse(JSON.stringify(this.filter));
        for(const av of this.available) {
            const q = copy.queryables[av.key];

            if (q.length === av.values.length)
                delete copy.queryables[av.key];
        }

        this.onSearch.emit(copy);
    }

    toggleEl(el: HTMLElement) {
        el.classList.toggle('closed');
    }
}
