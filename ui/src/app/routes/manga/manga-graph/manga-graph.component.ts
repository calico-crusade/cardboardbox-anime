import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { MangaGraph, MangaService, SubscriptionHandler } from 'src/app/services';

const STATES = ['favourite', 'completed', 'inprogress', 'bookmarked', 'else', 'touched', 'all' ];

@Component({
    templateUrl: './manga-graph.component.html',
    styleUrls: ['./manga-graph.component.scss']
})
export class MangaGraphComponent implements OnInit, OnDestroy {

    private _subs = new SubscriptionHandler();

    loading: boolean = false;
    error?: string;

    graph: MangaGraph[] = [];
    state: 'favourite' | 'completed' | 'inprogress' | 'bookmarked' | 'else' | 'touched' | 'all' = 'completed';

    states = STATES;

    constructor(
        private api: MangaService,
        private route: ActivatedRoute,
        private router: Router
    ) { }

    ngOnInit(): void {
        this._subs
            .subscribe(this.route.params, t => {
                this.state = t['state'];

                if (STATES.indexOf(this.state.toLowerCase()) === -1)
                    this.state = 'completed';

                this.process();
            });
    }

    ngOnDestroy(): void {
        this._subs.unsubscribe();
    }

    trigger() {
        this.router.navigate(['/manga', 'graph', this.state]);
    }

    private process() {
        this.loading = true;
        this.api
            .graph(this.state)
            .subscribe(t => {
                this.graph = t;
                this.loading = false;
            });
    }
}
