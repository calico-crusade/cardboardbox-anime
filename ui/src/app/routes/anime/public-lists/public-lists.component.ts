import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { AnimeService, PublicList } from '../../../services';

const DEFAULT_PAGE = 1,
      DEFAULT_SIZE = 100;

@Component({
    selector: 'cba-public-lists',
    templateUrl: './public-lists.component.html',
    styleUrls: ['./public-lists.component.scss']
})
export class PublicListsComponent implements OnInit {

    page: number = DEFAULT_PAGE;
    size: number = DEFAULT_SIZE;

    results: PublicList[] = [];
    total: number = 0;

    loading: boolean = true;

    get hasPrevPage() {
        const current = (this.page - 1) * this.size;
        return current > 0;
    }

    get hasNextPage() {
        const current = this.page * this.size;
        return current < this.total;
    }

    get pages() {
        return Math.ceil(this.total / this.size);
    }

    constructor(
        private api: AnimeService,
        private router: Router,
        private route: ActivatedRoute
    ) { }

    ngOnInit(): void {
        this.route.queryParams.subscribe(t => {
            let { page, size } = t;

            if (!page || page < 1) page = DEFAULT_PAGE;
            if (!size || size < 1) size = DEFAULT_SIZE;
            this.page = +page;
            this.size = +size;
            this.load();
        });
    }

    private load() {
        this.loading = true;
        this.api.listsPublic(this.page, this.size)
            .subscribe(t => {
                const { results, total } = t;
                this.results = results;
                this.total = total;
                this.loading = false;
            });
    }

    move(increment: number) {
        this.router.navigate(['/public-lists' ], {
            queryParams: {
                page: this.page + increment,
                size: this.size
            }
        });
    }

    go(list: PublicList) {
        this.router.navigate([ '/anime', list.listId ]);
    }
}
