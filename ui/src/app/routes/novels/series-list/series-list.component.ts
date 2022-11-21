import { Component, OnInit } from '@angular/core';
import { catchError, of } from 'rxjs';
import { LightNovelService, NovelSeries } from 'src/app/services';

const DEFAULT_PAGE = 1,
      DEFAULT_SIZE = 10;

@Component({
  templateUrl: './series-list.component.html',
  styleUrls: ['./series-list.component.scss']
})
export class SeriesListComponent implements OnInit {

    private _page: number = DEFAULT_PAGE;
    private _size: number = DEFAULT_SIZE;

    loading: boolean = false;
    books: NovelSeries[] = [];
    total: number = 0;
    pages: number = 0;


    constructor(
        private _api: LightNovelService
    ) { }

    ngOnInit(): void {
        this._page = DEFAULT_PAGE;
        this._size = DEFAULT_SIZE;
        this.loading = true;
        this.process();
    }

    private process() {
        this.loading = true;
        this._api
            .series(this._page, this._size)
            .error(() => {}, { results: [], count: 0, pages: 0 })
            .subscribe(t => {
                const { results, count, pages } = t;
                this.total = count;
                this.pages = pages;
                this.books = [...this.books, ...results];
                this.loading = false;
            });
    }

    onScroll() {
        if (this.total === this.books.length &&
            this.books.length !== 0) return;

        this._page += 1;
        this.process();
    }

    showMore(target: any, btn: any) {
        btn.target.innerText = btn.target.innerText === 'More...' ? 'Less...' : 'More...';
        target.classList.toggle('show-all');
    }

}
