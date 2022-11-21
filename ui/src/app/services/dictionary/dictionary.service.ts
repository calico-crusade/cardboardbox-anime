import { HttpClient } from "@angular/common/http";
import { Injectable } from "@angular/core";
import { catchError, of } from "rxjs";
import { Definition } from "./dictionary.model";

@Injectable({
    providedIn: 'root'
})
export class DictionaryService {

    constructor(
        private http: HttpClient
    ) { }

    get(text: string) {
        return this.http
            .get<Definition[]>('https://api.dictionaryapi.dev/api/v2/entries/en/' + text)
            .pipe(
                catchError(error => {
                    console.error('Error getting dictionary definition', { error, text });
                    return of(<Definition[]>[]);
                })
            );
    }
}