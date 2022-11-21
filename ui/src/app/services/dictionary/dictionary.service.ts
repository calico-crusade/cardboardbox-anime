import { Injectable } from "@angular/core";
import { HttpService } from "../http.service";
import { Definition } from "./dictionary.model";

@Injectable({
    providedIn: 'root'
})
export class DictionaryService {

    constructor(
        private http: HttpService
    ) { }

    get(text: string) {
        return this.http
            .get<Definition[]>('https://api.dictionaryapi.dev/api/v2/entries/en/' + text)
            .error(() => {}, []);
    }
}