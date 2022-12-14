import { Injectable } from "@angular/core";
import { of, tap } from "rxjs";
import { ConfigObject } from "../config.base";
import { HttpService, RxjsHttpResp } from "../http.service";
import { ChapterPages, NovelBook, NovelChapter, NovelSeries, PagesResult, Scaffold, SeriesResult } from "./lightnovels.model";

@Injectable({
    providedIn: 'root'
})
export class LightNovelService extends ConfigObject {

    private _scaffoldCache: { [key: number]: Scaffold } = {};

    constructor(
        private http: HttpService
    ) { super(); }

    series(page: number = 1, size: number = 100) {
        return this.http.get<SeriesResult>(`novels`, { params: { page, size }});
    }

    seriesById(seriesId: number) {
        const url = `${this.apiUrl}/novels/${seriesId}`;
        return this.cacheItem(seriesId, url, this._scaffoldCache);
    }

    booksBySeriesId(seriesId: number) {
        return this.http.get<NovelBook[]>(`novels/${seriesId}/books`);
    }

    chapters(bookId: number) {
        return this.http.get<NovelChapter>(`novels/${bookId}/chapters`);
    }

    pages(seriesId: number, page: number = 1, size: number = 100) {
        return this.http.get<PagesResult>(`novels/${seriesId}/pages`, { params: { page, size }});
    }

    corsFallback(url: string, group: string = 'anime', referer?: string) {
        var path = encodeURIComponent(url);
        let uri = `https://cba-proxy.index-0.com/proxy?path=${path}&group=${group}`;

        if (referer) uri += `&referer=${encodeURIComponent(referer)}`;

        return uri;
    }

    downloadUrl(series: NovelSeries): string;
    downloadUrl(book: NovelBook): string;
    downloadUrl(id: number, type: ('series' | 'book')): string;
    downloadUrl(item: NovelSeries | NovelBook | number, type?: ('series' | 'book')) {
        const book = (id: number) => `${this.apiUrl}/novels/${id}/epub`;
        const series = (id: number) => `${this.apiUrl}/novels/${id}/epubs`;

        if (typeof item === 'number') {
            if (type === 'book') return book(item);
            return series(item);
        }

        if ('seriesId' in item) return book(item.id);
        return series(item.id);
    }

    load(url: string): RxjsHttpResp<{ count: number, isNew: boolean }>;
    load(id: number): RxjsHttpResp<{ count: number, isNew: boolean }>;
    load(item: string | number) {
        let params: { [key: string]: any } = typeof item === 'string' ? { url: item } : { seriesId: item };
        return this.http.get<{ count: number, isNew: boolean }>(`novels/load`, { params });
    }

    invalidateCache() {
        this._scaffoldCache = {};
    }

    chapter(bookId: number, chapterId: number) {
        return this.http.get<ChapterPages[]>(`novel/${bookId}/chapter/${chapterId}`);
    }

    private cacheItem<T>(id: number, url: string, cache: { [key: number]: T }) {
        if (cache[id]) return of(cache[id]);

        return this.http
            .get<T>(url)
            .observable
            .pipe(
                tap(t => { cache[id] = t; })
            );
    }
}