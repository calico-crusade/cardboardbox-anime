import { Component, OnInit } from '@angular/core';
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
                .error(error => this.error = error.toString())
                .subscribe(t => {
                    this.res = t;
                    this.loading = false;       
                });
    }

    ngOnInit(): void {
    }

}
