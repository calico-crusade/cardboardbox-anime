import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

@Component({
    templateUrl: './install-instructions.component.html',
    styleUrls: ['./install-instructions.component.scss']
})
export class InstallInstructionsComponent implements OnInit {

    type?: string;

    constructor(
        private route: ActivatedRoute
    ) { }

    ngOnInit(): void {
        this.route.params.subscribe(t => {
            this.type = t['type'];
        });
    }

}
