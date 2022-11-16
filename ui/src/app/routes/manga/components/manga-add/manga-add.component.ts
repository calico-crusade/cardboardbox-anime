import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, of } from 'rxjs';
import { MangaService } from 'src/app/services';

@Component({
    selector: 'cba-manga-add',
    templateUrl: './manga-add.component.html',
    styleUrls: ['./manga-add.component.scss']
})
export class MangaAddComponent implements OnInit {

    url: string = '';
    loading: boolean = false;


    constructor(
        private api: MangaService,
        private router: Router,
    ) { }

    ngOnInit(): void {
    }

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
