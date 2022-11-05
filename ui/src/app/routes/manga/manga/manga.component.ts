import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { catchError, of } from 'rxjs';
import { Manga, MangaService } from 'src/app/services';

@Component({
    templateUrl: './manga.component.html',
    styleUrls: ['./manga.component.scss']
})
export class MangaComponent implements OnInit {

    loading: boolean = false;
    error?: string;
    url!: string;
    manga?: Manga;

    constructor(
        private route: ActivatedRoute,
        private api: MangaService
    ) { }

    ngOnInit(): void {
        this.route.params.subscribe(t => {
            this.url = t['url'];
            this.process();
        });
    }

    private process() {
        this.loading = true;
        this.api
            .manga(this.url)
            .pipe(
                catchError(err => {
                    
                    return of(undefined);
                })
            )
            .subscribe(t => {
                this.manga = t;
                this.loading = false;
            });
    }

}
