import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { PagedResults, Filters, FilterSearch, ListExt, ListPost, ListPut, Id, Anime, List, ListMapItem, PublicLists, Chapter, Book } from './anime.model';
import { ConfigObject } from '../config.base';
import { BehaviorSubject, combineLatestWith, map, Observable, switchMap, tap } from 'rxjs';
import { HttpService, RxjsHttpResp } from '../http.service';

export type ListsMaps = {
    lists: { [key: number]: ListExt },
    listsMap: { [key: number]: number[] },
    animeMap: { [key: number]: number[] }
}

@Injectable({
    providedIn: 'root'
})
export class AnimeService extends ConfigObject {

    private _map = new BehaviorSubject<ListsMaps | undefined>(undefined);

    get map() {
        const cur = this._map.getValue();
        if (!cur)
            return this.buildMap()
                .pipe(
                    switchMap(_ => this._map.asObservable())
                );

        return this._map.asObservable();
    }

    constructor(
        private http: HttpService,
        private rawhttp: HttpClient
    ) { super(); }

    search(search: FilterSearch) {
        search.mature = +search.mature || 0;
        return this.http.post<PagedResults<Anime>>(`anime`, search);
    }

    filters() {
        return this.http.get<Filters>(`anime/filters`);
    }

    buildMap() {
        const mapOs = this.mapsGet();
        const listOs = this.listsGet();

        return mapOs.observable.pipe(
            combineLatestWith(listOs.observable),
            map(([ maps, lists ]) => {
                const output: ListsMaps = {
                    lists: { },
                    listsMap: { },
                    animeMap: { }
                };

                for(const map of maps) {
                    const list = lists.find(t => t.id === map.listId);
                    if (!list) continue;

                    output.lists[list.id] = list;
                    output.listsMap[list.id] = map.animeIds;

                    for(const ai of map.animeIds) {
                        if (!output.animeMap[ai])
                            output.animeMap[ai] = [];

                        output.animeMap[ai].push(list.id);
                    }
                }

                return output;
            }),
            tap(t => this._map.next(t))
        );
    }

    listsGet() { return this.http.get<ListExt[]>(`lists`); }
    
    listsPublic(): RxjsHttpResp<PublicLists>;
    listsPublic(page: number, size: number): RxjsHttpResp<PublicLists>;
    listsPublic(id: number): RxjsHttpResp<ListExt>;
    listsPublic(list: ListPut): RxjsHttpResp<ListExt>;
    listsPublic(list?: number | ListPut, size?: number) {
        if (!list) return this.http.get<PublicLists>(`lists/public`);

        if (typeof list === 'number' && size && typeof size === 'number')
            return this.http.get<PublicLists>(`lists/public`, { params: { page: list, size } });

        if (typeof list !== 'number') list = list.id;
        return this.http.get<ListExt>(`lists/public/${list}`);
    }


    listsPost(list: ListPost) { 
        return this.http
            .post<Id>(`lists`, list)
            .observable
            .pipe(
                switchMap(t => 
                    this.listsGet().observable
                        .pipe(
                            map(a => a.find(z => z.id === t.id))
                        )
                )
            ); 
    }

    listsPut(list: ListPut) { 
        return this.rawhttp
            .put(`${this.apiUrl}/lists`, list).pipe(
                switchMap(_ => this.buildMap())
            );
    }

    listsDelete(id: number): Observable<any>;
    listsDelete(list: ListPut): Observable<any>;
    listsDelete(par: number | ListPut) {
        if (typeof par !== 'number') par = par.id;
        return this.rawhttp
            .delete(`${this.apiUrl}/lists/${par}`)
            .pipe(
                switchMap(_ => this.buildMap())
            );
    }

    mapsGet() { return this.http.get<ListMapItem[]>(`list-map`); }

    mapsToggle(animeId: number, listId: number): Observable<{ inList: boolean }>;
    mapsToggle(anime: Anime, listId: number): Observable<{ inList: boolean }>;
    mapsToggle(animeId: number, list: List): Observable<{ inList: boolean }>;
    mapsToggle(anime: Anime, list: List): Observable<{ inList: boolean }>;
    mapsToggle(anime: number | Anime, list: number | List) {
        if (typeof anime !== 'number') anime = anime.id;
        if (typeof list !== 'number') list = list.id;
        return this.http
            .get<{ inList: boolean }>(`list-map/${list}/${anime}`)
            .observable
            .pipe(
                switchMap(t => this.buildMap().pipe(map(_ => t)))
            );
    }

    lightnovel(id: string, page: number = 1, size: number = 10) {
        return this.http.get<PagedResults<Chapter>>(`ln/${id}`, { params: { page, size }});
    }

    lightnovels(page: number = 1, size: number = 100) {
        return this.http.get<PagedResults<Book>>(`ln`, { params: { page, size }});
    }
}
