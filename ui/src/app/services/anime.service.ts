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
        return this.http.post<PagedResults>(`${this.apiUrl}/anime`, search);
    }

    page(page: number = 1, size: number = 50, asc: boolean = true) {
        return this.http.get<PagedResults>(`${this.apiUrl}/anime`, {
            params: { page, size, asc }
        });
    }

    filters() {
        return this.http.get<Filters>(`${this.apiUrl}/anime/filters`);
    }
}
