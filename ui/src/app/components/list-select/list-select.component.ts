import { Component, Injectable, OnInit } from '@angular/core';
import { lastValueFrom, Subject, tap } from 'rxjs';
import { Anime, AnimeService, AuthService, AuthUser, ListExt, ListsMaps, UtilitiesService } from './../../services';

@Component({
    selector: 'cba-list-select',
    templateUrl: './list-select.component.html',
    styleUrls: ['./list-select.component.scss']
})
export class ListSelectComponent implements OnInit {

    /**
     * Whether or not we have an active API request
     */
    loading: boolean = false;
    /**
     * Whether or not this modal is open
     */
    open: boolean = false;
    /**
     * The currently authorized user (null if not logged in)
     */
    curUser?: AuthUser;
    /**
     * The modal's state
     */
    cur?: Target;
    /** 
     * The current user's lists & their @see {@link Anime} reference ids 
     */
    map?: ListsMaps;
    /**
     * The text input string for title
     */
    newTitle: string = '';
    /**
     * The text input string for the description
     */
    newDescription: string = '';
    /** 
     * Gets all of the @see {@link ListExt} the user has 
     */
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
        //When the modal is opened
        this.srv.onOpened.subscribe(t => {
            this.cur = t;
            this.open = true;
        });
        
        //When the logged in state changes
        this.auth.onLogin.subscribe(t => {
            this.curUser = t;
            if (!this.curUser) return;

            //Trigger a fetch of the user's lists
            this.api.map.subscribe(t => this.map = t);
        });
    }

    /**
     * Close / Cancel the current modal
     */
    cancel() {
        this.cur?.rej('canceled');
        this.open = false;
    }

    /**
     * Close / Success for the modal
     * @param list The list that was chosen
     */
    ok(list: ListExt) {
        this.cur?.res(list);
        this.open = false;
    }

    /**
     * Whether or not the given list has the current anime in it
     * @param item The list that we're checking
     * @returns Whether or not the list is has the current anime
     */
    exists(item: ListExt) {
        if (!this.cur?.context || !this.map) return false;

        const ids = this.map.listsMap[item.id];
        if (!ids) return false;

        return this.utils.any(ids, this.cur.context.id);
    }

    /**
     * "Create" button - creates a list using @see newTitle and @see newDescription
     */
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