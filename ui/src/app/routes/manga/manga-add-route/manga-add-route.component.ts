import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { MangaService, SubscriptionHandler } from './../../../services';

@Component({
    templateUrl: './manga-add-route.component.html',
    styleUrls: ['./manga-add-route.component.scss']
})
export class MangaAddRouteComponent implements OnInit, OnDestroy {

    private _subs = new SubscriptionHandler();

    loading: boolean = false;

    constructor(
        private route: ActivatedRoute,
        private router: Router,
        private api: MangaService
    ) { }

    ngOnInit(): void {
        this._subs
            .subscribe(this.route.queryParams, (t) => {
                const url = t['url'];
                if (!url) return;

                this.add(url);
            });
    }

    ngOnDestroy(): void {
        this._subs.unsubscribe();
    }

    private add(url: string) {
        this.loading = true;
        this.api
            .manga(url)
            .error(err => {
                alert('An error occurred while trying to load your manga!');
            })
            .subscribe(t => {
                this.loading = false;
                if (!t) return;

                this.router.navigate(['/manga', t.manga.id]);
            });
    }
}
