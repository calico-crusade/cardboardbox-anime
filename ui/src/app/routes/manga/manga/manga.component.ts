import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { catchError, of } from 'rxjs';
import { Manga, MangaService, MangaWithChapters } from 'src/app/services';

@Component({
    templateUrl: './manga.component.html',
    styleUrls: ['./manga.component.scss']
})
export class MangaComponent implements OnInit {

    loading: boolean = false;
    error?: string;
    id!: number;
    data?: MangaWithChapters;

    get manga() {
        return this.data?.manga;
    }

    get chapters() {
        return this.data?.chapters || [];
    }

    constructor(
        private route: ActivatedRoute,
        private api: MangaService
    ) { }

    ngOnInit(): void {
        this.route.params.subscribe(t => {
            this.id = +t['id'];
            this.process();
        });
    }

    private process() {
        this.loading = true;
        this.api
            .manga(this.id)
            .pipe(
                catchError(err => {
                    return of(undefined);
                })
            )
            .subscribe(t => {
                this.data = t;
                this.loading = false;
            });
    }

    update() {
        if (!this.manga) return;

        this.loading = true;
        this.api
            .reload(this.manga)
            .pipe(
                catchError(err => {
                    this.error = 'An error occurred while refreshing the manga!';
                    console.error('Error occurred!', {
                        manga: this.manga,
                        chapters: this.chapters,
                        id: this.id,
                        err
                    })
                    return of(undefined);
                })
            )
            .subscribe(t => {
                this.data = t;
                this.loading = false;
            })
    }

}
