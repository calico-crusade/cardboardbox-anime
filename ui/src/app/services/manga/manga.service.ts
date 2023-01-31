import { Injectable } from "@angular/core";
import { ActivatedRoute } from "@angular/router";
import { BehaviorSubject, of } from "rxjs";
import { Filters } from "../anime/anime.model";
import { ConfigObject } from "../config.base";
import { HttpService, RxjsHttpResp } from "../http.service";
import { StorageVar } from "../storage-var";
import { 
    ImageSearch, Manga, MangaChapter, 
    MangaFilter, MangaProgress, MangaProgressData, 
    MangaProgressUpdate, MangaStripReq, MangaWithChapters, 
    PaginatedManga, PaginatedMangaProgress 
} from "./manga.model";
import { DateTime } from 'luxon';
import { AuthService } from "../auth/auth.service";

@Injectable({
    providedIn: 'root'
})
export class MangaService extends ConfigObject {

    private _searchSub = new BehaviorSubject<boolean>(false);
    private _lastCheck = new StorageVar<string | undefined>(undefined, 'manga-last-check');

    get onSearch() { return this._searchSub.asObservable(); }

    get isSearching() { return this._searchSub.getValue(); }
    set isSearching(value: boolean) { this._searchSub.next(value); }

    get lastCheck() { 
        const value = this._lastCheck.value; 
        if (!value) return undefined;
        return DateTime.fromISO(value); 
    }
    set lastCheck(value: DateTime | undefined) { this._lastCheck.value = value?.toISO(); }

    constructor(
        private http: HttpService,
        private route: ActivatedRoute,
        private auth: AuthService
    ) { super(); }

    manga(id: number): RxjsHttpResp<MangaWithChapters>;
    manga(id: number, chapter: number): RxjsHttpResp<string[]>;
    manga(url: string): RxjsHttpResp<MangaWithChapters>;
    manga(idUrl: number | string, chapter?: number) {
        if (!idUrl && !chapter) return undefined;

        if (idUrl && typeof idUrl === 'number' && !chapter) return this.http.get<MangaWithChapters>(`manga/${idUrl}`);
        if (idUrl && typeof idUrl === 'string' && !chapter) {
            if (idUrl.toLocaleLowerCase().startsWith('http')) return this.http.get<MangaWithChapters>(`manga/load`, { params: { url: idUrl }});
            return this.http.get<MangaWithChapters>(`manga/${idUrl}`);
        }
        return this.http.get<string[]>(`manga/${idUrl}/${chapter}/pages`);
    }

    mangaExtended(id: number): RxjsHttpResp<MangaProgressData>;
    mangaExtended(id: string): RxjsHttpResp<MangaProgressData>;
    mangaExtended(id: number | string) {
        return this.http.get<MangaProgressData>(`manga/${id}/extended`);
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

    progress(id: number): RxjsHttpResp<MangaProgress>;
    progress(progress: MangaProgressUpdate): RxjsHttpResp<any>;
    progress(item: number | MangaProgressUpdate) {
        if (typeof item === 'number') return this.http.get<MangaProgress>(`manga/${item}/progress`);
        return this.http.post<any>(`manga`, item);
    }

    filters() {
        return this.http.get<Filters>(`manga/filters`);
    }

    search(search: MangaFilter) {
        let filter = this.clone(search);

        if (filter.nsfw === undefined && this.auth.currentUser)
            filter.nsfw = 2;

        return this.http.post<PaginatedMangaProgress>(`manga/search`, filter);
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

    routeFilter(state?: number) {
        const t = this.route.snapshot.queryParams;

        let filter: MangaFilter = {
            page: 1,
            size: 20,
            search: '',
            include: [],
            exclude: [],
            asc: true,
            sort: 0,
            state: state || 0
        };

        if (t['search']) filter.search = t['search'];
        if (t['desc']) filter.asc = false;
        if (t['include']) filter.include = t['include'].split(',');
        if (t['exclude']) filter.exclude = t['exclude'].split(',');
        if (t['sort']) filter.sort = +t['sort'];
        if (t['state'] && state === undefined) filter.state = +t['state'];
        if (t['nsfw']) filter.nsfw = +t['nsfw'];

        return filter;
    }

    routerParameters(search: MangaFilter, overrideState: number) {
        let pars: { [key: string]: any } = {};

        if (search.search) pars['search'] = search.search;
        if (search.include.length > 0) pars['include'] = search.include.join(',');
        if (search.exclude.length > 0) pars['exclude'] = search.exclude.join(',');
        if (!search.asc) pars['desc'] = true;
        if (search.sort) pars['sort'] = search.sort;
        if (search.state && overrideState < 0) pars['state'] = search.state;
        if (search.nsfw) pars['nsfw'] = search.nsfw;

        return pars;
    }

    strip(req: MangaStripReq) { return this.http.download('manga/strip', req); }

    since(date: Date, page: number = 1, size: number = 100) {
        return this.http.get<PaginatedMangaProgress>(`manga/since/${date.toISOString()}`, { params: { page, size }});
    }

    promptCheck(): RxjsHttpResp<PaginatedMangaProgress> {
        const now = DateTime.now(),
              check = this.lastCheck,
              def = new RxjsHttpResp<PaginatedMangaProgress>(of({ results: [], pages: 0, count: 0 }), 'prompt-check');

        if (!this.auth.currentUser) return def;
        if (!check) { this.lastCheck = now; return def; }

        const diff = now.diff(check, 'minutes').minutes;
        if (diff <= 5) { return def; }

        const date = check.toJSDate();
        return this.since(date)
            .tap(() => this.lastCheck = now);
    }

    resetProgress(mangaId: number) {
        return this.http.delete(`manga/progress/${mangaId}`);
    }

    clone<T>(item: T): T { return JSON.parse(JSON.stringify(item)); }

    imageSearch(url: string): RxjsHttpResp<ImageSearch>;
    imageSearch(file: File): RxjsHttpResp<ImageSearch>;
    imageSearch(item: string | File) {
        if (typeof item === 'string') {
            return this.http.get<ImageSearch>(`manga/image-search`, { params: { path: item } });
        }

        const data = new FormData();
        data.append('file', item);
        return this.http.post<ImageSearch>(`manga/image-search`, data);
    }
}