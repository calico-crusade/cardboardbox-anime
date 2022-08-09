import { Component, Injectable, OnInit } from '@angular/core';
import { lastValueFrom, Subject, tap } from 'rxjs';
import { Anime, AnimeService, AuthService, AuthUser, ListExt, ListsMaps, UtilitiesService } from './../../services';

@Component({
    selector: 'cba-list-select',
    templateUrl: './list-select.component.html',
    styleUrls: ['./list-select.component.scss']
})
export class ListSelectComponent implements OnInit {

    loading: boolean = false;
    open: boolean = false;
    curUser?: AuthUser;
    cur?: Target;
    map?: ListsMaps;

    newTitle: string = '';
    newDescription: string = '';

    get lists() {
        if (!this.map?.lists) return [];

        const output = [];
        for(const key in this.map.lists)
            output.push(this.map.lists[key]);
        
        return output;
    }

    constructor(
        private srv: ListSelectService,
        private api: AnimeService,
        private auth: AuthService,
        private utils: UtilitiesService
    ) { }

    async ngOnInit() {
        this.srv.onOpened.subscribe(t => {
            this.cur = t;
            this.open = true;
        });
        
        this.auth.onLogin.subscribe(t => {
            this.curUser = t;
            if (!this.curUser) return;

            this.api.map.subscribe(t => this.map = t);
        });
    }

    cancel() {
        this.cur?.rej('canceled');
        this.open = false;
    }

    ok(list: ListExt) {
        this.cur?.res(list);
        this.open = false;
    }

    exists(item: ListExt) {
        if (!this.cur?.context || !this.map) return false;

        const ids = this.map.listsMap[item.id];
        if (!ids) return false;

        return this.utils.any(ids, this.cur.context.id);
    }

    async create() {
        try {
            this.loading = true;
            const list = await lastValueFrom(this.api.listsPost({
                title: this.newTitle,
                description: this.newDescription
            }));

            this.newTitle = '';
            this.newDescription = '';

            if (!list) {
                this.loading = false;
                return;
            }

            this.ok(list);
            this.loading = false;
        } catch (e) {
            console.error('List creation error', { e });
            this.loading = false;
        }
    }
}

interface Target {
    res: (list: ListExt) => void;
    rej: (reason: string) => void;
    context?: Anime;
}

@Injectable({ providedIn: 'root' })
export class ListSelectService {

    private _sub = new Subject<Target>();

    get onOpened() { return this._sub.asObservable(); }

    open(context?: Anime) {
        return new Promise<ListExt>((res, rej) => {
            const target: Target = { res, rej, context };
            this._sub.next(target);
        });
    }
}