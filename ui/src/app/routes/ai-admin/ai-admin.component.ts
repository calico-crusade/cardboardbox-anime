import { Component, OnInit } from '@angular/core';
import { catchError, of } from 'rxjs';
import { AiService } from 'src/app/services';

@Component({
    templateUrl: './ai-admin.component.html',
    styleUrls: ['./ai-admin.component.scss']
})
export class AiAdminComponent implements OnInit {

    images: { url: string, show: boolean }[] = [];

    constructor(
        private api: AiService
    ) { }

    ngOnInit() {
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

}
