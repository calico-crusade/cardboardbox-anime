import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { catchError, of } from 'rxjs';
import { LightNovelService, Scaffold } from './../../../services/lightnovels';
import { saveAs } from 'file-saver';

@Component({
    templateUrl: './series.component.html',
    styleUrls: ['./series.component.scss']
})
export class SeriesComponent implements OnInit {

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
        private route: ActivatedRoute
    ) { }

    ngOnInit(): void {
        this.route.params.subscribe(t => {
            this.id = +t['id'];
            this.process();
        });
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
                this.loading = false;
            });
    }

    download() {
        if (!this.series) return;

        this.downloading = true;
        const url = this.api.downloadUrl(this.series);

        this.api
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
