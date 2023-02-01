import { Component, Injectable, OnDestroy, OnInit } from '@angular/core';
import { lastValueFrom, Subject } from 'rxjs';
import { AnimeService, Anime, AuthService, AuthUser, ListsMaps, SubscriptionHandler } from './../../services';
import { ListSelectService } from '../list-select/list-select.component';

const DEF_IMG = '/assets/default-background.webp';

@Component({
    selector: 'cba-anime-modal',
    templateUrl: './anime-modal.component.html',
    styleUrls: ['./anime-modal.component.scss']
})
export class AnimeModalComponent implements OnInit, OnDestroy {

    private _subs = new SubscriptionHandler();

    /** 
     * The @see {@link Anime} object to display in the modal 
     */
    anime?: Anime;
    /** 
     * Whether or not the modail is open */
    open: boolean = false;
    /** 
     * The currently authed user (undefined if not logged in) 
     */
    curUser?: AuthUser;
    /** 
     * The current user's lists & their @see {@link Anime} reference ids 
     */
    map?: ListsMaps

    /** 
     * Converts @see {@link Anime.languageTypes} from "Dubbed/Subbed" to "Dub/Sub" and removes "Unknown" 
     */
    get langs() {
        if (!this.anime) return [];

        return this.getLangs(this.anime);
    }

    /** 
     * Gets all of the @see {@link ListExt} the user has 
     */
    get inLists() {
        if (!this.map || !this.map?.lists || !this.anime) return [];
        const ani = this.map.animeMap[this.anime.id];
        if (!ani) return [];
        return ani.map(t => this.map?.lists[t]);
    }

    constructor(
        private srv: AnimeModalService,
        private lists: ListSelectService,
        private api: AnimeService,
        private auth: AuthService
    ) { }

    getLangs(anime: Anime) {
        return anime
            .languageTypes
            .filter(t => t !== 'Unknown')
            .map(t => {
                switch(t) {
                    case 'Dubbed': return 'Dub';
                    case 'Subbed': return 'Sub';
                    default: return t;
                }
            })
            .sort();
    }

    ngOnInit(): void {
        this._subs
            //When the modal gets opened
            .subscribe(this.srv.onClicked, t => {
                this.anime = t;
                this.open = true;
            })
            //When the logged in state changes
            .subscribe(this.auth.onLogin, t => {
                this.curUser = t;
                if (!this.curUser) return;
                //Trigger a fetch of the user's lists
                this.api.map.subscribe(t => this.map = t);
            });
    }

    ngOnDestroy(): void { this._subs.unsubscribe(); }

    /** 
     * Closes & Cancels the modal 
     */
    close() {
        this.open = false;
        this.anime = undefined;
    }

    /**
     * Gets the anime's poster to display
     * @returns the poster's url
     */
    getImage() {
        if (!this.anime) return DEF_IMG;
        const sort = this.anime
            .images
            .filter(t => t.type === 'poster')
            .sort((a, b) => (a.width ?? 0) - (b.width ?? 0));
        if (sort.length === 0) return DEF_IMG;
        const last = sort[sort.length - 1];
        return last.source;
    }

    /**
     * "Add To List" button logic
     * @returns Promise for when the modal is closed
     */
    async addToList() {
        if (!this.anime || !this.curUser || !this.map) return;

        try {
            //Open the modal for modifying the lists
            const list = await this.lists.open(this.anime);
            if (!list) return;
            //Trigger the toggle API request to add or remove from lists
            const ob = this.api.mapsToggle(this.anime, list);
            const { inList } = await lastValueFrom(ob);
        } catch (e) {
            console.error('Watch lists closed', { e });
        }
    }
}

@Injectable({
    providedIn: 'root'
})
export class AnimeModalService {

    private _sub: Subject<Anime> = new Subject();

    get onClicked() { return this._sub.asObservable(); }

    constructor() { }

    openAnime(anime: Anime) {
        this._sub.next(anime);
    }
}