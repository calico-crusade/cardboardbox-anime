import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { SubscriptionHandler } from 'src/app/services';

@Component({
    templateUrl: './install-instructions.component.html',
    styleUrls: ['./install-instructions.component.scss']
})
export class InstallInstructionsComponent implements OnInit, OnDestroy {

    private _subs = new SubscriptionHandler();

    type?: string;

    constructor(
        private route: ActivatedRoute
    ) { }

    ngOnInit(): void {
        this._subs.subscribe(this.route.params, t => {
            this.type = t['type'];
        });
    }

    ngOnDestroy(): void {
        this._subs.unsubscribe();
    }

}
