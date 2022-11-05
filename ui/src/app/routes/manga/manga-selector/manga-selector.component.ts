import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';

@Component({
    templateUrl: './manga-selector.component.html',
    styleUrls: ['./manga-selector.component.scss']
})
export class MangaSelectorComponent implements OnInit {

    url: string = '';

    constructor(
        private router: Router
    ) { }

    ngOnInit(): void {
    }

    submit() {
        this.router.navigate(['/manga', this.url ]);
    }
}
