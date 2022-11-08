import { Component, OnDestroy, OnInit } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { catchError, of } from 'rxjs';
import { AiService } from 'src/app/services';

@Component({
  templateUrl: './ai-all-images.component.html',
  styleUrls: ['./ai-all-images.component.scss']
})
export class AiAllImagesComponent implements OnInit, OnDestroy {

    images: { url: string, show: boolean }[] = [];

    constructor(
        private api: AiService,
        private title: Title
    ) { }

    ngOnInit() {
        this.title.setTitle('CBA | All Gened Images');
        this.api
            .images()
            .pipe(
                catchError(err => {
                    console.error('Error occurred fetching admin images', { err });
                    return of([]);
                })
            )
            .subscribe(t => {
                this.images = t.map(t => {
                    return {
                        url: `${this.api.apiUrl}/${t}`,
                        show: false
                    }
                });
            });
    }

    ngOnDestroy(): void {
        this.title.setTitle(this.api.defaultTitle);
    }

}
