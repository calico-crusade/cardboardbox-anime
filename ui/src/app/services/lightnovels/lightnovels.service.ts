import { HttpClient, HttpResponse } from "@angular/common/http";
import { Injectable } from "@angular/core";
import { Observable, of, tap } from "rxjs";
import { ConfigObject } from "../config.base";
import { NovelBook, NovelChapter, NovelSeries, PagesResult, Scaffold, SeriesResult } from "./lightnovels.model";
import { saveAs } from "file-saver";


@Injectable({
    providedIn: 'root'
})
export class LightNovelService extends ConfigObject {

    private _scaffoldCache: { [key: number]: Scaffold } = {};

    constructor(
        private http: HttpClient
    ) { super(); }

    series(page: number = 1, size: number = 100) {
        return this.http.get<SeriesResult>(`${this.apiUrl}/novels`, { params: { page, size }});
    }

    seriesById(seriesId: number) {
        const url = `${this.apiUrl}/novels/${seriesId}`;
        return this.cacheItem(seriesId, url, this._scaffoldCache);
    }

    booksBySeriesId(seriesId: number) {
        return this.http.get<NovelBook[]>(`${this.apiUrl}/novels/${seriesId}/books`);
    }

    chapters(bookId: number) {
        return this.http.get<NovelChapter>(`${this.apiUrl}/novels/${bookId}/chapters`);
    }

    pages(seriesId: number, page: number = 1, size: number = 100) {
        return this.http.get<PagesResult>(`${this.apiUrl}/novels/${seriesId}/pages`, { params: { page, size }});
    }

    corsFallback(url: string) {
        return `${this.apiUrl}/misc/file?path=${encodeURIComponent(url)}`;
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

    download(url: string): Observable<HttpResponse<Blob>> {
        return this.http.get(url, {
            observe: 'response',
            responseType: 'blob'
        }).pipe(
            tap(t => {
                const filename = t.headers.get('content-disposition')
                    ?.split(';')[1]
                    .split('filename')[1]
                    .split('=')[1]
                    .trim();

                if (t.body) saveAs(t.body, filename);
            })
        );
    }

    load(url: string): Observable<{ count: number, isNew: boolean }>;
    load(id: number): Observable<{ count: number, isNew: boolean }>;
    load(item: string | number) {
        let params: { [key: string]: any } = typeof item === 'string' ? { url: item } : { seriesId: item };
        return this.http.get<{ count: number, isNew: boolean }>(`${this.apiUrl}/novels/load`, { params });
    }

    invalidateCache() {
        this._scaffoldCache = {};
    }

    private cacheItem<T>(id: number, url: string, cache: { [key: number]: T }) {
        if (cache[id]) return of(cache[id]);

        return this.http
            .get<T>(url)
            .pipe(
                tap(t => { cache[id] = t; })
            );
    }
}