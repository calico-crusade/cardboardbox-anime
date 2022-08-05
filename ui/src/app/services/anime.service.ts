import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { VrvAnime, PagedResults, Filters, FilterSearch } from './anime.model';
import { environment } from 'src/environments/environment';
import { Observable, shareReplay } from 'rxjs';

@Injectable({
    providedIn: 'root'
})
export class AnimeService {

    private cache$?: Observable<VrvAnime[]>;

    private get apiUrl() { return environment.apiUrl; }

    constructor(
        private http: HttpClient
    ) { }

    all() {
        if (!this.cache$)
            this.cache$ = this.http
                .get<VrvAnime[]>(`${this.apiUrl}/vrv/all`)
                .pipe(shareReplay());
        return this.cache$;
    }

    search(search: FilterSearch) {
        search.mature = +search.mature;
        return this.http.post<PagedResults>(`${this.apiUrl}/anime/all`, search);
    }

    page(page: number = 1, size: number = 50, asc: boolean = true) {
        return this.http.get<PagedResults>(`${this.apiUrl}/anime/all`, {
            params: { page, size, asc }
        });
    }

    filters() {
        return this.http.get<Filters>(`${this.apiUrl}/anime/filters`);
    }
}
