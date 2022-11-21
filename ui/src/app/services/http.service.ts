import { HttpClient, HttpContext, HttpHeaders, HttpParams } from "@angular/common/http";
import { Injectable } from "@angular/core";
import { catchError, lastValueFrom, Observable, of } from "rxjs";
import { environment } from "src/environments/environment";
import { ConfigObject } from "./config.base";

export type HttpOptions = {
    headers?: HttpHeaders | {
        [header: string]: string | string[];
    };
    context?: HttpContext;
    observe?: 'body';
    params?: HttpParams | {
        [param: string]: string | number | boolean | ReadonlyArray<string | number | boolean>;
    };
    reportProgress?: boolean;
    responseType?: 'json';
    withCredentials?: boolean;
}

export class RxjsHttpResp<T> {

    url!: string;
    observable!: Observable<T>;
    rawObservable!: Observable<T>;

    get promise() { return lastValueFrom(this.observable); }

    constructor(
        _observable: Observable<T>,
        _url: string
    ) { 
        this.url = _url;
        this.observable = _observable;
        this.rawObservable = _observable;
    }

    error(handler: (err: any) => void, def?: T) {
        this.observable = <any>this.observable
            .pipe(
                catchError(err => {
                    console.error('Error occurred during XHR: ', {
                        url: this.url,
                        error: err,
                        default: def
                    });
                    handler(err);
                    return of(def);
                })
            );
        return this;
    }

    subscribe(handler: (item: T) => void) { 
        return this.observable.subscribe(t => {
            if (!environment.production)
                console.log('XHR Request Result', {
                    url: this.url,
                    results: t
                });
            handler(t);
        });
    }
}

@Injectable({ providedIn: 'root' })
export class HttpService extends ConfigObject {

    constructor(
        public http: HttpClient
    ) { super(); }

    formatUrl(url: string) {
        if (url.indexOf('https://') !== -1 ||
            url.indexOf('http://') !== -1) return url;

        if (url.startsWith('/'))
            url = url.substring(1);

        return `${this.apiUrl}/${url}`;
    }

    get<T>(url: string, options?: HttpOptions) {
        let req = this.http.get<T>(
            this.formatUrl(url),
            options
        );
        return new RxjsHttpResp<T>(req, url);
    }

    post<T>(url: string, body: any, options?: HttpOptions) {
        let req = this.http.post<T>(
            this.formatUrl(url),
            body,
            options
        );
        return new RxjsHttpResp<T>(req, url);
    }
}