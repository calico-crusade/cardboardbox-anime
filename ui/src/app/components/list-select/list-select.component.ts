import { Component, Injectable, OnInit } from '@angular/core';
import { lastValueFrom, Subject, tap } from 'rxjs';
import { Anime, AnimeService, ListExt } from './../../services';

@Component({
    selector: 'cba-list-select',
    templateUrl: './list-select.component.html',
    styleUrls: ['./list-select.component.scss']
})
export class ListSelectComponent implements OnInit {

    loading: boolean = false;
    open: boolean = false;
    cur?: Target;
    lists: ListExt[] = [];

    newTitle: string = '';
    newDescription: string = '';

    constructor(
        private srv: ListSelectService,
        private api: AnimeService
    ) { }

    async ngOnInit() {
        this.srv.onOpened.subscribe(t => {
            this.cur = t;
            this.open = true;
        });
        this.api.lists.subscribe(t => {
            this.lists = t;
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
        if (!this.cur || !this.cur.context) return false;

        const exists = this.cur.context.find(t => t.id === item.id);
        return !!exists;
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
    context?: ListExt[];
}

@Injectable({ providedIn: 'root' })
export class ListSelectService {

    private _sub = new Subject<Target>();

    get onOpened() { return this._sub.asObservable(); }

    open(context?: ListExt[]) {
        return new Promise<ListExt>((res, rej) => {
            const target: Target = { res, rej, context };
            this._sub.next(target);
        });
    }
}