import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { catchError, of } from 'rxjs';
import { LightNovelService, Scaffold } from './../../../services/lightnovels';
import { Title } from '@angular/platform-browser';
import { AuthService, SubscriptionHandler, UtilitiesService } from 'src/app/services';

@Component({
    templateUrl: './series.component.html',
    styleUrls: ['./series.component.scss']
})
export class SeriesComponent implements OnInit, OnDestroy {

    private _subs = new SubscriptionHandler();

    id: number = 0;
    scaffold?: Scaffold;
    loading: boolean = true;
    error?: string;
    downloading: boolean = false;

    get source() { 
        if (!this.series) return '';
        return new URL(this.series.url || '').hostname.replace('www.', '');
    }
    get series() { return this.scaffold?.series; }
    get books() { return this.scaffold?.books || []; }

    constructor(
        private api: LightNovelService,
        private route: ActivatedRoute,
        private title: Title,
        private auth: AuthService,
        private util: UtilitiesService
    ) { }

    ngOnInit(): void {
        this._subs.subscribe(this.route.params, t => {
            this.id = +t['id'];
            this.process();
        });
    }

    ngOnDestroy(): void {
        this.title.setTitle(this.api.defaultTitle);
        this.auth.title = undefined;
        this._subs.unsubscribe();
    }

    private process() {
        this.loading = true;
        this.error = undefined;
        this.api
            .seriesById(this.id)
            .pipe(
                catchError(err => {
                    console.error('Error occurred while fetching series', {
                        id: this.id,
                        err
                    });
                    this.error = '404: Unable to find series!';
                    return of(undefined);
                })
            )
            .subscribe(t => {
                this.scaffold = t;
                this.title.setTitle(this.scaffold?.series.title || 'CardboardBox | Anime');
                this.auth.title = this.scaffold?.series.title || '';
                this.loading = false;
            });
    }

    download() {
        if (!this.series) return;

        this.downloading = true;
        const url = this.api.downloadUrl(this.series);

        this.util
            .download(url)
            .pipe(
                catchError(error => {
                    console.error('Error occurred while downloading file: ', {
                        error,
                        url,
                        scaffold: this.scaffold
                    });
                    this.error = 'An error occurred while attempting to download your file(s)!';
                    setTimeout(() => {
                        this.error = undefined;
                    }, 3000);
                    return of(undefined);
                })
            )
            .subscribe(t => {
                this.downloading = false;
            });
    }
}
