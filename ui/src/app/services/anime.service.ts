import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { PagedResults, Filters, FilterSearch } from './anime.model';
import { environment } from 'src/environments/environment';

@Injectable({
    providedIn: 'root'
})
export class AnimeService {
    private get apiUrl() { return environment.apiUrl; }

    constructor(
        private http: HttpClient
    ) { }

    search(search: FilterSearch) {
        search.mature = +search.mature || 0;
        return this.http.post<PagedResults>(`${this.apiUrl}/anime/v2`, search);
    }

    filters() {
        return this.http.get<Filters>(`${this.apiUrl}/anime/v2/filters`);
    }
}
