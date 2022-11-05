import { HttpClient } from "@angular/common/http";
import { Injectable } from "@angular/core";
import { ConfigObject } from "../config.base";
import { Manga, MangaChapter } from "./manga.model";

@Injectable({
    providedIn: 'root'
})
export class MangaService extends ConfigObject {

    constructor(
        private http: HttpClient
    ) { super(); }

    manga(url: string) {
        return this.http.get<Manga>(`${this.apiUrl}/manga`, {
            params: { url }
        });
    }

    chapter(url: string, chapter: string) {
        return this.http.get<MangaChapter>(`${this.apiUrl}/manga/chapter`, {
            params: { url, chapter }
        });
    }
}