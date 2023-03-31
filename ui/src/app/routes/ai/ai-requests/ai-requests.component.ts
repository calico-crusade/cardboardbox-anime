import { Component, OnDestroy, OnInit } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { Router } from '@angular/router';
import { AiDbRequest, AiService, AuthService, SubscriptionHandler } from 'src/app/services';

@Component({
    templateUrl: './ai-requests.component.html',
    styleUrls: ['./ai-requests.component.scss']
})
export class AiRequestsComponent implements OnInit, OnDestroy {

    private _subs = new SubscriptionHandler();

    loading: boolean = false;
    error?: string;

    id?: number;
    page: number = 1;
    size: number = 10;

    total: number = 0;
    pages: number = 0;
    res: AiDbRequest[] = [];

    get isAdmin() {
        return !!(this.auth.currentUser?.roles || []).find(t => t === 'Admin');
    }

    constructor(
        private api: AiService,
        private auth: AuthService,
        private router: Router,
        private title: Title
    ) { }

    ngOnInit() {
        this.title.setTitle('CBA | Image Gen History');

        this._subs.subscribe(this.auth.onLogin, t => this.process());
        this.process();
    }

    ngOnDestroy(): void {
        this.title.setTitle(this.api.defaultTitle);
        this._subs.unsubscribe();
    }

    process() {
        this.loading = true;
        this.error = undefined;
        this.pages = 0;
        this.total = 0;
        this.res = [];

        const id = this.isAdmin && !this.id ? -1 : this.id;

        this.api
            .requests(id, this.page, this.size)
            .error(err => {
                let code = err.status;
                if (code === 401) {
                    this.error = 'Unauthorized! You need to be logged in to do this!';
                } else {
                    this.error = err.statusText || 'Unknown Error! Contact an admin!';
                }
            }, { pages: 0, count: 0, results: [] })
            .subscribe(t => {
                const { pages, count, results } = t;
                this.pages = pages;
                this.total = count;
                this.res = results;
                this.loading = false;
            });
    }

    imageUrl(url: string) {
        return `${this.api.apiUrl}/${url}`;
    }

    bgimage(url: string) {
        const imgUrl = this.imageUrl(url);
        return `background-image: url(${imgUrl}), url(/assets/broken.webp)`;
    }

    move(increment: number) {
        this.page += increment;
        this.process();
    }

    open(req: AiDbRequest) {
        this.api.reload(req);
        this.router.navigate(['/ai']);
    }
}
