import { Component, OnInit } from '@angular/core';
import { ListsMaps, AuthUser, AnimeService, AuthService, ListExt } from './../../services';

@Component({
    selector: 'cba-lists',
    templateUrl: './lists.component.html',
    styleUrls: ['./lists.component.scss']
})
export class ListsComponent implements OnInit {

    /**
     * Whether or not we have an active API request
     */
    loading: boolean = false;
    /** 
     * The current user's lists & their @see {@link Anime} reference ids 
     */
    map?: ListsMaps;
    /**
     * The currently authorized user (null if not logged in)
     */
    curUser?: AuthUser;
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
        private api: AnimeService,
        private auth: AuthService,
    ) { }

    ngOnInit(): void {
        this.auth.onLogin.subscribe(t => {
            this.curUser = t;
            if (!this.curUser) return;
            this.api.map.subscribe(t => this.map = t);
        });
    }

    remove(list: ListExt) {
        this.loading = true;
        this.api
            .listsDelete(list)
            .subscribe(_ => {
                this.loading = false;
            });
    }
}
