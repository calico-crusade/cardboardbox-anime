import { Component, Injectable, OnInit } from '@angular/core';
import { lastValueFrom, Subject } from 'rxjs';
import { AnimeService, Anime, AuthService, AuthUser, ListExt } from './../../services';
import { ListSelectService } from '../list-select/list-select.component';

const DEF_IMG = '/assets/default-background.webp';

@Component({
    selector: 'cba-anime-modal',
    templateUrl: './anime-modal.component.html',
    styleUrls: ['./anime-modal.component.scss']
})
export class AnimeModalComponent implements OnInit {

    anime?: Anime;
    open: boolean = false;
    curUser?: AuthUser;
    inLists: ListExt[] = [];

    get langs() {
        if (!this.anime) return [];

        return this.anime
            .languageTypes
            .filter(t => t !== 'Unknown')
            .map(t => {
                switch(t) {
                    case 'Dubbed': return 'Dub';
                    case 'Subbed': return 'Sub';
                    default: return t;
                }
            });
    }

    constructor(
        private srv: AnimeModalService,
        private lists: ListSelectService,
        private api: AnimeService,
        private auth: AuthService
    ) { }

    ngOnInit(): void {
        this.srv.onClicked.subscribe(t => {
            this.anime = t;
            this.inLists = [];
            if (!!this.curUser)
                this.api
                    .listsGetByAnime(t.id)
                    .subscribe(t => this.inLists = t);
            this.open = true;
        });
        this.auth.onLogin.subscribe(t => this.curUser = t);
    }

    close() {
        this.open = false;
        this.anime = undefined;
    }

    getBackgroundImage() {
        if (!this.anime) return DEF_IMG;

        const sort = this.anime
            .images
            .filter(t => t.type === 'poster')
            .sort((a, b) => (a.width ?? 0) - (b.width ?? 0));

        if (sort.length === 0) return DEF_IMG;

        const last = sort[sort.length - 1];
        return last.source;
    }

    async watchlist() {
        if (!this.anime || !this.curUser) return;

        try {
            const list = await this.lists.open(this.inLists);
            if (!list) return;

            const i = this.inLists.findIndex(t => t.id === list.id);
            if (i !== -1) {
                const exists = this.inLists[i];
                await lastValueFrom(this.api.mapDelete(this.anime.id, exists.id));
                this.inLists.splice(i, 1);
                console.log('Anime removed from watch list', {
                    list,
                    exists,
                    anime: this.anime
                })
                return;
            }

            await lastValueFrom(this.api.mapPost({
                listId: list.id,
                animeId: this.anime?.id
            }));

            this.inLists.push(list);

            console.log('Anime added to watch list', { 
                list,
                anime: this.anime
             });
        } catch (e) {
            console.log('Watch lists closed', { e });
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