import { Injectable } from '@angular/core';
import { Anime, List, ListMapItem } from './anime/anime.model';
import { saveAs } from "file-saver";
import { HttpClient, HttpResponse } from '@angular/common/http';
import { Observable, tap } from "rxjs";

@Injectable({
    providedIn: 'root'
})
export class UtilitiesService {

    constructor(
        private http: HttpClient
    ) { }

    all<T>(data: T[], pred: (item: T) => boolean) {
        for(let i of data) {
            if (!pred(i)) return false;
        }

        return true;
    }

    any<T>(data: T[], item: T) {
        for(let i of data) {
            if (i === item) return true;
        }

        return false;
    }

    anyOf<T>(data: T[], want: T[]) {
        for(let i of data) {
            for(let w of want) {
                if (w === i) return true;
            }
        }

        return false;
    }

    indexInBounds<T>(data: T[], next: number) {
        if (next < 0) return data.length - 1;
        if (next >= data.length) return 0;
        return next;
    }

    ran(max: number, min: number = 0) {
        return Math.floor(Math.random() * max) + min;
    }

    rand<T>(data: T[]) {
        return data[this.ran(data.length)];
    }

    lists(maps: ListMapItem[], animeId: number): number[]
    lists(maps: ListMapItem[], anime: Anime): number[]
    lists(maps: ListMapItem[], anime: number | Anime) {
        if (typeof anime !== 'number') anime = anime.id;

        return maps
            .filter(t => this.any(t.animeIds, anime))
            .map(t => t.listId);
    }

    anime(maps: ListMapItem[], listId: number): number[]
    anime(maps: ListMapItem[], list: List): number[]
    anime(maps: ListMapItem[], list: number | List) {
        if (typeof list !== 'number') list = list.id;
        
        const l = maps.find(t => t.listId === list);
        if (!l) return [];
        return l.animeIds;
    }

    exists(map: ListMapItem[], listId: number, animeId: number): boolean
    exists(map: ListMapItem[], list: List, animeId: number): boolean;
    exists(map: ListMapItem[], listId: number, anime: Anime): boolean;
    exists(map: ListMapItem[], list: List, anime: Anime): boolean;
    exists(map: ListMapItem[], list: number | List, anime: number | Anime) {
        if (typeof list !== 'number') list = list.id;
        if (typeof anime !== 'number') anime = anime.id;

        const ids = this.anime(map, list);
        return this.any(ids, anime);
    }

    download(url: string): Observable<HttpResponse<Blob>> {
        return this.http.get(url, {
            observe: 'response',
            responseType: 'blob'
        }).pipe(
            tap(t => {
                const filename = t.headers.get('content-disposition')
                    ?.split(';')[1]
                    .split('filename')[1]
                    .split('=')[1]
                    .trim();

                if (t.body) saveAs(t.body, filename);
            })
        );
    }
}
