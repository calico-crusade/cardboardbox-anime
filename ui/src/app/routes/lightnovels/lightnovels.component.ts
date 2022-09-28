import { Component, OnInit } from '@angular/core';
import { AnimeService, Book } from './../../services';

@Component({
    templateUrl: './lightnovels.component.html',
    styleUrls: ['./lightnovels.component.scss']
})
export class LightnovelsComponent implements OnInit {

    page: number = 1;
    size: number = 100;
    loading: boolean = false;

    results: Book[] = [];
    pages: number = 0;
    count: number = 0;

    constructor(
        private api: AnimeService
    ) { }

    ngOnInit(): void {
        this.results = [];
        this.process();
    }

    private process() {
        this.loading = true;
        this.api.lightnovels(this.page, this.size)
            .subscribe(t => {
                const { count, pages, results } = t;
                this.results.push(...results);
                this.count = count;
                this.pages = pages;
                this.loading = false;
            });
    }

    onScroll() {
        this.page += 1;
        this.process();
    }
}
