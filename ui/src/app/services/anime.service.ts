import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { PagedResults, Filters, FilterSearch, ListExt, ListPost, ListPut, Id, ListMap, Anime, List } from './anime.model';
import { ConfigObject } from './config.base';
import { BehaviorSubject, map, Observable, switchMap, tap } from 'rxjs';

@Injectable({
    providedIn: 'root'
})
export class AnimeService extends ConfigObject {

    private _lists = new BehaviorSubject<ListExt[]>([]);

    get lists() { 
        const cur = this._lists.getValue();
        if (!cur || cur.length === 0)
            return this.listsGet()
                .pipe(
                    switchMap(_ => this._lists.asObservable())
                );

        return this._lists.asObservable(); 
    }

    constructor(
        private http: HttpClient
    ) { super(); }

    search(search: FilterSearch) {
        search.mature = +search.mature || 0;
        return this.http.post<PagedResults>(`${this.apiUrl}/anime/v2`, search);
    }

    filters() {
        return this.http.get<Filters>(`${this.apiUrl}/anime/v2/filters`);
    }

    listsGet() { 
        return this.http
            .get<ListExt[]>(`${this.apiUrl}/lists`)
            .pipe(
                tap(t => this._lists.next(t))
            ); 
    }

    listsGetByAnime(animeId: number): Observable<ListExt[]>;
    listsGetByAnime(anime: Anime): Observable<ListExt[]>;
    listsGetByAnime(anime: Anime | number) {
        if (typeof anime !== 'number') anime = anime.id;
        return this.http.get<ListExt[]>(`${this.apiUrl}/lists/${anime}`);
    }

    listsPost(list: ListPost) { 
        return this.http
            .post<Id>(`${this.apiUrl}/lists`, list)
            .pipe(
                switchMap(t => 
                    this.listsGet()
                        .pipe(
                            map(a => a.find(z => z.id === t.id))
                        )
                )
            ); 
    }

    listsPut(list: ListPut) { 
        return this.http
            .put(`${this.apiUrl}/lists`, list).pipe(
                switchMap(_ => this.listsGet())
            );
    }

    listsDelete(id: number): Observable<any>;
    listsDelete(list: ListPut): Observable<any>;
    listsDelete(par: number | ListPut) {
        if (typeof par !== 'number') par = par.id;
        return this.http
            .delete(`${this.apiUrl}/lists/${par}`)
            .pipe(
                switchMap(_ => this.listsGet())
            );
    }

    mapPost(list: ListMap) { 
        return this.http
            .post(`${this.apiUrl}/list-map`, list)
            .pipe(
                switchMap(t => this.listsGet()
                    .pipe(
                        map(_ => t)
                    ))
            ); 
    }

    mapDelete(anime: number, list: number): Observable<any>;
    mapDelete(anime: Anime, list: number): Observable<any>;
    mapDelete(anime: Anime, list: List): Observable<any>;
    mapDelete(anime: number, list: List): Observable<any>;
    mapDelete(anime: number | Anime, list: number | List) {
        if (typeof anime !== 'number') anime = anime.id;
        if (typeof list !== 'number') list = list.id;
        return this.http
            .delete(`${this.apiUrl}/list-map/${list}/${anime}`)
            .pipe(
                switchMap(t => this.listsGet().pipe(map(_ => t)))
            );
    }
}
