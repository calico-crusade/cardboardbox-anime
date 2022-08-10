import { HttpClient, HttpEvent, HttpHandler, HttpInterceptor, HttpRequest } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { BehaviorSubject, lastValueFrom, Observable } from 'rxjs';
import { AnimeService } from './anime.service';
import { AuthCodeResponse, AuthUser } from './auth.model';
import { ConfigObject } from './config.base';

const SKIP_URIS: string[] = [];

@Injectable({
    providedIn: 'root'
})
export class AuthService extends ConfigObject {

    private _loginSub = new BehaviorSubject<AuthUser | undefined>(undefined);

    get onLogin() { return this._loginSub.asObservable(); }
    get currentUser() { return this._loginSub.getValue(); }

    constructor(
        private http: HttpClient,
        private api: AnimeService
    ) { super(); }

    async bump() {
        if (!this.token) return false;

        try {
            let me = await lastValueFrom(this.me());
            await lastValueFrom(this.api.buildMap());
            this._loginSub.next(me);
            return true;
        } catch (e) {
            console.warn('Error occurred while fetching profile', { e });
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