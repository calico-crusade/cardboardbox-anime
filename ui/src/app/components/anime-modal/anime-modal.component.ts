import { Component, Injectable, OnInit } from '@angular/core';
import { Subject } from 'rxjs';
import { Anime } from 'src/app/services/anime.model';

const DEF_IMG = '/assets/default-background.webp';

@Component({
    selector: 'cba-anime-modal',
    templateUrl: './anime-modal.component.html',
    styleUrls: ['./anime-modal.component.scss']
})
export class AnimeModalComponent implements OnInit {

    anime?: Anime;
    open: boolean = false;

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
        private srv: AnimeModalService
    ) { }

    ngOnInit(): void {
        this.srv.onClicked.subscribe(t => {
            this.anime = t;
            this.open = true;
        });
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

    watchlist() {
        alert('Not implemented yet :(');
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