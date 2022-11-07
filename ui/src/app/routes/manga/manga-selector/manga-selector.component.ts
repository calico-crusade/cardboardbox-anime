import { Component, OnInit, ViewChild } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, of } from 'rxjs';
import { PopupComponent, PopupService } from 'src/app/components';
import { Manga, MangaService } from 'src/app/services';

@Component({
    templateUrl: './manga-selector.component.html',
    styleUrls: ['./manga-selector.component.scss']
})
export class MangaSelectorComponent implements OnInit {

    loading: boolean = false;

    data: Manga[] = [];
    
    page: number = 1;
    size: number = 10;
    pages: number = 0;

    url: string = '';

    @ViewChild('popup') popup!: PopupComponent;

    constructor(
        private api: MangaService,
        private pop: PopupService,
        private router: Router
    ) { }

    ngOnInit() {
        this.process();
    }

    private process() {
        this.api
            .allManga(this.page, this.size)
            .subscribe(t => {
                const { results, pages } = t;
                this.pages = pages;
                this.data = [ ...this.data, ...results ];
            });
    }

    onScroll() {
        this.page += 1;
        if (this.pages < this.page) return;

        this.process();
    }

    add() { this.pop.show(this.popup); }

    load() {
        this.loading = true;
        this.api
            .manga(this.url)
            .pipe(
                catchError(err => {
                    console.error('Error occurred while loading manga', {
                        url: this.url,
                        err
                    });
                    alert('An error occurred while trying to load your manga!');
                    return of(undefined);
                })
            )
            .subscribe(t => {
                this.loading = false;
                if (!t) return;

                this.router.navigate(['/manga', t.manga.id]);
            });
    }
}
