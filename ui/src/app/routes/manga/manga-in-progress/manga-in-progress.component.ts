import { Component, OnDestroy, OnInit } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { catchError, of } from 'rxjs';
import { AuthService, LightNovelService, MangaProgressData, MangaService } from 'src/app/services';

@Component({
  templateUrl: './manga-in-progress.component.html',
  styleUrls: ['./manga-in-progress.component.scss']
})
export class MangaInProgressComponent implements OnInit, OnDestroy {

    loading: boolean = false;
    error?: string;

    data: MangaProgressData[] = [];

    constructor(
        private api: MangaService,
        private auth: AuthService,
        private title: Title,
        private lnApi: LightNovelService
    ) { }

    ngOnInit() {
        this.title.setTitle('CardboardBox | In Progress Manga');
        this.process();
        this.auth.onLogin.subscribe(t => this.process());
    }

    ngOnDestroy() {
        this.title.setTitle(this.api.defaultTitle);
    }

    proxy(url?: string) {
        if (!url) return '';
        return this.lnApi.corsFallback(url);
    }

    private process() {
        if (!this.auth.currentUser) return;

        this.loading = true;
        this.api
            .inProgress()
            .pipe(
                catchError(err => {
                    console.error('Error occurred while fetching in-progress', { err });
                    return of([]);
                })
            )
            .subscribe(t => {
                this.data = t;
                this.loading = false;
            })
    }
}
