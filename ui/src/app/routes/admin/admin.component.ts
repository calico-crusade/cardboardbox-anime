import { Component, OnInit } from '@angular/core';
import { catchError, of } from 'rxjs';
import { LightNovelService } from './../../services';

@Component({
    templateUrl: './admin.component.html',
    styleUrls: ['./admin.component.scss']
})
export class AdminComponent implements OnInit {

    loading: boolean = false;
    url: string = '';
    error?: string;
    id: number = 0;
    res: any;

    constructor(
        private api: LightNovelService
    ) { }

    load(item: string | number) {
        this.loading = true;
        (typeof item === 'string' ? 
            this.api.load(item) : 
            this.api.load(item))
                .pipe(
                    catchError(error => {
                        this.error = error.toString();
                        setTimeout(() => {
                            this.error = undefined;
                        }, 3000);
                        return of();
                    })
                )
                .subscribe(t => {
                    this.res = t;
                    this.loading = false;       
                });
    }

    ngOnInit(): void {
    }

}
