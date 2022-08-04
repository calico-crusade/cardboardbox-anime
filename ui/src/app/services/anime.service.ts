import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { VrvAnime } from './anime.model';
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
}
