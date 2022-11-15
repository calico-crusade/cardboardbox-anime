import { HttpClient, HttpEvent, HttpHandler, HttpInterceptor, HttpRequest } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { BehaviorSubject, lastValueFrom, Observable } from 'rxjs';
import { AnimeService } from '../anime/anime.service';
import { AuthCodeResponse, AuthUser } from './auth.model';
import { ConfigObject } from '../config.base';

const SKIP_URIS: string[] = [];
const STORAGE_REROUTE = 'reroute-source';

@Injectable({
    providedIn: 'root'
})
export class AuthService extends ConfigObject {

    private _loginSub = new BehaviorSubject<AuthUser | undefined>(undefined);
    private _titleSub = new BehaviorSubject<string | undefined>(undefined);
    private _loggingInSub = new BehaviorSubject<boolean>(false);
    private _headerSub = new BehaviorSubject<boolean>(true);

    get onLogin() { return this._loginSub.asObservable(); }
    get currentUser() { return this._loginSub.getValue(); }

    get onTitleChange() { return this._titleSub.asObservable(); }
    get title() { return this._titleSub.getValue(); }
    set title(title: string | undefined) {
        this._titleSub.next(title);
    }

    get isLoggingIn() { return this._loggingInSub.value; }
    set isLoggingIn(val: boolean) { this._loggingInSub.next(val); }
    get onIsLoggingIn() { return this._loggingInSub.asObservable(); }

    get onHeaderChange() { return this._headerSub.asObservable(); }
    get showHeader() { return this._headerSub.getValue(); }
    set showHeader(value: boolean) { this._headerSub.next(value); }

    get lastRoute(){ return localStorage.getItem(STORAGE_REROUTE); }
    set lastRoute(value: string | null) {
        if (value === '/' || !value) return;
        localStorage.setItem(STORAGE_REROUTE, value);
    }

    constructor(
        private http: HttpClient,
        private api: AnimeService
    ) { super(); }

    async bump() {
        this.isLoggingIn = true;
        if (!this.token) {
            this.isLoggingIn = false;
            return false;
        }

        try {
            let me = await lastValueFrom(this.me());
            await lastValueFrom(this.api.buildMap());
            this._loginSub.next(me);
            this.isLoggingIn = false;
            return true;
        } catch (e) {
            console.warn('Error occurred while fetching profile', { e });
            this.isLoggingIn = false;
            return false;
        }
    }

    async login(): Promise<AuthCodeResponse> {
        let code = await this.doLoginPopup();
        if (!code) return { error: 'Invalid Login Code' };

        let auth = await lastValueFrom(this.resolve(code));
        if (!auth || auth.error || !auth.token) {
            this.token = null;
            return auth || { error: 'Error occurred while logging in' };
        }

        this.token = auth.token;
        await lastValueFrom(this.api.buildMap());
        this._loginSub.next(auth.user);
        return auth;
    }

    logout() {
        this.token = null;
        this._loginSub.next(undefined);
    }

    private async doLoginPopup() {
        let timer: any;
        let instance: any;

        var promise = new Promise<{ code: string }>((res, rej) => {

            window.addEventListener('message', (event) => {
                if (event.origin !== 'https://auth.index-0.com') return;
                res(event.data);
            });

            instance = window.open("https://auth.index-0.com/Home/Auth/" + this.appId,
                "cardboard_oauth_login_window",
                `toolbar=no,location=no,status=no,menubar=no,scrollbars=yes,resizable=yes,width=750,height=500`);

            timer = setInterval(() => {
                if (!instance.closed) return;

                clearInterval(timer);
                rej('Window Closed');
            }, 1000);
        });

        try {
            var token = await promise;
            clearInterval(timer);
            instance.close();

            return token?.code;
        } catch (ex) {
            console.warn('Error occurred during oauth', { ex, instance, timer });
            return undefined;
        }
    }

    private resolve(code: string) { return this.http.get<AuthCodeResponse>(`${this.apiUrl}/auth/${code}`); }

    private me() { return this.http.get<AuthUser>(`${this.apiUrl}/auth`); }
}

@Injectable()
export class AuthInterceptor extends ConfigObject implements HttpInterceptor {

    intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
        const token = this.token;

        for(const url of SKIP_URIS) {
            if (req.url.indexOf(url) !== -1)
                return next.handle(req);
        }

        let headers = req.headers;

        if (token) {
            headers = headers.set('authorization', `Bearer ${token}`);
        }

        const r = req.clone({ headers });

        return next.handle(r);
    }
}