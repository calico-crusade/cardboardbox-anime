import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { PagedResults, Filters, FilterSearch, ListExt, ListPost, ListPut, Id, ListMap, Anime, List } from './anime.model';
import { ConfigObject } from './config.base';
import { Observable } from 'rxjs';

@Injectable({
    providedIn: 'root'
})
export class AnimeService extends ConfigObject {
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

    listsGet() { return this.http.get<ListExt[]>(`${this.apiUrl}/lists`); }
    listsPost(list: ListPost) { return this.http.post<Id>(`${this.apiUrl}/lists`, list); }
    listsPut(list: ListPut) { return this.http.put(`${this.apiUrl}/lists`, list); }
    listsDelete(id: number): Observable<any>;
    listsDelete(list: ListPut): Observable<any>;
    listsDelete(par: number | ListPut) {
        if (typeof par !== 'number') par = par.id;
        return this.http.delete(`${this.apiUrl}/lists/${par}`);
    }

    mapPost(list: ListMap) { return this.http.post<Id>(`${this.apiUrl}/list-map`, list); }
    mapDelete(anime: number, list: number): Observable<any>;
    mapDelete(anime: Anime, list: number): Observable<any>;
    mapDelete(anime: Anime, list: List): Observable<any>;
    mapDelete(anime: number, list: List): Observable<any>;
    mapDelete(anime: number | Anime, list: number | List) {
        if (typeof anime !== 'number') anime = anime.id;
        if (typeof list !== 'number') list = list.id;
        return this.http.delete(`${this.apiUrl}/list-map/${list}/${anime}`);
    }
}
