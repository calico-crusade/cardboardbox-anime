import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { MangaService } from './../../../../services';

@Component({
    selector: 'cba-manga-add',
    templateUrl: './manga-add.component.html',
    styleUrls: ['./manga-add.component.scss']
})
export class MangaAddComponent {

    url: string = '';
    loading: boolean = false;


    constructor(
        private api: MangaService,
        private router: Router,
    ) { }

    load() {
        this.loading = true;
        this.api
            .manga(this.url)
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
