import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { SubscriptionHandler } from './../../services';

@Component({
    templateUrl: './about.component.html',
    styleUrls: ['./about.component.scss']
})
export class AboutComponent implements OnInit, OnDestroy {

    private _subs = new SubscriptionHandler();

    constructor(
        private router: Router,
        private route: ActivatedRoute
    ) { }

    ngOnDestroy(): void {
        this._subs.unsubscribe();
    }

    ngOnInit(): void {
        this._subs
            .subscribe(this.route.queryParams, t => {
                const id = t['id'];
                if (!id) return;

                const el = document.getElementById(id);
                if (!el) return;

                el.scrollIntoView({ behavior: 'smooth' });
            });
    }
}
