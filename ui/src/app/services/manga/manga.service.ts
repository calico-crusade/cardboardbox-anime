import { HttpClient } from "@angular/common/http";
import { Injectable } from "@angular/core";
import { Observable } from "rxjs";
import { Filters } from "../anime/anime.model";
import { ConfigObject } from "../config.base";
import { Manga, MangaChapter, MangaFilter, MangaProgress, MangaProgressData, MangaProgressUpdate, MangaWithChapters, PaginatedManga } from "./manga.model";

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

    reload(manga: Manga): Observable<MangaWithChapters>;
    reload(url: string): Observable<MangaWithChapters>;
    reload(item: Manga | string) {
        if (typeof item !== 'string') item = item.url;
        return this.http.get<MangaWithChapters>(`${this.apiUrl}/manga/load`, { params: { url: item, force: true }});
    }

    allManga(page: number, size: number) {
        return this.http.get<PaginatedManga>(`${this.apiUrl}/manga`, { params: { page, size }});
    }

    inProgress() {
        return this.http.get<MangaProgressData[]>(`${this.apiUrl}/manga/in-progress`);
    }

    progress(id: number): Observable<MangaProgress>;
    progress(progress: MangaProgressUpdate): Observable<any>;
    progress(item: number | MangaProgressUpdate) {
        if (typeof item === 'number') return this.http.get<MangaProgress>(`${this.apiUrl}/manga/${item}/progress`);
        return this.http.post<any>(`${this.apiUrl}/manga`, item);
    }

    filters() {
        return this.http.get<Filters>(`${this.apiUrl}/manga/filters`);
    }

    search(filter: MangaFilter) {
        return this.http.post<PaginatedManga>(`${this.apiUrl}/manga/search`, filter);
    }

    favourite(id: number) {
        return this.http.get<boolean>(`${this.apiUrl}/manga/${id}/favourite`);
    }

    bookmark(chapter: MangaChapter, pages: number[]) {
        return this.http.post(`${this.apiUrl}/manga/${chapter.mangaId}/${chapter.id}/bookmark`, pages);
    }
}