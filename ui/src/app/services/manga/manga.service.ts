import { HttpClient } from "@angular/common/http";
import { Injectable } from "@angular/core";
import { Observable } from "rxjs";
import { ConfigObject } from "../config.base";
import { MangaWithChapters, PaginatedManga } from "./manga.model";

@Injectable({
    providedIn: 'root'
})
export class MangaService extends ConfigObject {

    constructor(
        private http: HttpClient
    ) { super(); }

    manga(id: number): Observable<MangaWithChapters>;
    manga(id: number, chapter: number): Observable<string[]>;
    manga(url: string): Observable<MangaWithChapters>;
    manga(idUrl: number | string, chapter?: number) {
        if (idUrl && typeof idUrl === 'number' && !chapter) return this.http.get<MangaWithChapters>(`${this.apiUrl}/manga/${idUrl}`);
        if (idUrl && typeof idUrl === 'string' && !chapter) return this.http.get<MangaWithChapters>(`${this.apiUrl}/manga/load`, { params: { url: idUrl }});
        return this.http.get<string[]>(`${this.apiUrl}/manga/${idUrl}/${chapter}/pages`);
    }

    allManga(page: number, size: number) {
        return this.http.get<PaginatedManga>(`${this.apiUrl}/manga`, { params: { page, size }});
    }
}