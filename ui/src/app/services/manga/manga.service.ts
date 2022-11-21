import { Injectable } from "@angular/core";
import { Filters } from "../anime/anime.model";
import { ConfigObject } from "../config.base";
import { HttpService, RxjsHttpResp } from "../http.service";
import { Manga, MangaChapter, MangaFilter, MangaProgress, MangaProgressData, MangaProgressUpdate, MangaWithChapters, PaginatedManga, PaginatedMangaProgress } from "./manga.model";

@Injectable({
    providedIn: 'root'
})
export class MangaService extends ConfigObject {

    constructor(
        private http: HttpService
    ) { super(); }

    manga(id: number): RxjsHttpResp<MangaWithChapters>;
    manga(id: number, chapter: number): RxjsHttpResp<string[]>;
    manga(url: string): RxjsHttpResp<MangaWithChapters>;
    manga(idUrl: number | string, chapter?: number) {
        if (!idUrl && !chapter) return undefined;

        if (idUrl && typeof idUrl === 'number' && !chapter) return this.http.get<MangaWithChapters>(`manga/${idUrl}`);
        if (idUrl && typeof idUrl === 'string' && !chapter) return this.http.get<MangaWithChapters>(`manga/load`, { params: { url: idUrl }});
        return this.http.get<string[]>(`manga/${idUrl}/${chapter}/pages`);
    }

    reload(manga: Manga): RxjsHttpResp<MangaWithChapters>;
    reload(url: string): RxjsHttpResp<MangaWithChapters>;
    reload(item: Manga | string) {
        if (typeof item !== 'string') item = item.url;
        return this.http.get<MangaWithChapters>('/manga/load', { params: { url: item, force: true }});
    }

    allManga(page: number, size: number) {
        return this.http.get<PaginatedManga>(`manga`, { params: { page, size }});
    }

    inProgress() {
        return this.http.get<MangaProgressData[]>(`manga/in-progress`);
    }

    progress(id: number): RxjsHttpResp<MangaProgress>;
    progress(progress: MangaProgressUpdate): RxjsHttpResp<any>;
    progress(item: number | MangaProgressUpdate) {
        if (typeof item === 'number') return this.http.get<MangaProgress>(`manga/${item}/progress`);
        return this.http.post<any>(`manga`, item);
    }

    filters() {
        return this.http.get<Filters>(`manga/filters`);
    }

    search(filter: MangaFilter) {
        return this.http.post<PaginatedManga>(`manga/search`, filter);
    }

    searchV2(filter: MangaFilter) {
        return this.http.post<PaginatedMangaProgress>(`manga/search-v2`, filter);
    }

    favourite(id: number) {
        return this.http.get<boolean>(`manga/${id}/favourite`);
    }

    bookmark(chapter: MangaChapter, pages: number[]) {
        return this.http.post(`manga/${chapter.mangaId}/${chapter.id}/bookmark`, pages);
    }

    random() { return this.http.get<MangaWithChapters>(`manga/random`); }

    touched(page: number, size: number, type?: ('favourite' | 'completed' | 'inprogress' | 'bookmarked' | number)) {
        let params: { [key: string]: any } = { page, size };
        if (type) params['type'] = type;
        return this.http.get<PaginatedMangaProgress>(`manga/touched`, { params });
    }
}