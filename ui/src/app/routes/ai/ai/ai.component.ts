import { Component, OnDestroy, OnInit } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { ActivatedRoute, Router } from '@angular/router';
import { asyncScheduler, catchError, filter, map, observeOn, of, switchMap, tap } from 'rxjs';
import { AiRequestImg2Img, AiService, AuthService, SubscriptionHandler, DEFAULT_REQUEST } from 'src/app/services';

@Component({
    templateUrl: './ai.component.html',
    styleUrls: ['./ai.component.scss']
})
export class AiComponent implements OnInit, OnDestroy {

    private _subs = new SubscriptionHandler();

    loading: boolean = false;
    issues: string[] = [];
    error?: string;
    urls: string[] = [];

    img: boolean = false;
    hints: boolean = false;
    advanced: boolean = true;

    request: AiRequestImg2Img = this.clone(DEFAULT_REQUEST);

    id$ = this.route.params.pipe(
        map(t => <string | undefined>t['id']),
        filter(t => !!t),
        tap(_ => this.loading = true),
        switchMap(id => this.api.request(+(id || '0')).observable),
        catchError(err => {
            this.error = 'An error occurred while trying to fetch your image! Please try again later, or contact an administrator!';
            console.error('An error occurred while fetching AI image', { err });
            return of(undefined);
        }),
        tap(t => {
            this.loading = false;
            if (!t) return;

            this.request = t;
            this.img = !!t.image;
            this.request.image = t.image || '';
            this.request.denoiseStrength = t.denoiseStrength || 0.2;
            this.urls = t.outputPaths;
        }),
        map(t => t?.id)
    );

    embeddings$ = this.auth.onLogin.pipe(
        observeOn(asyncScheduler),
        filter(t => {
            this.error = undefined;
            if (t) return true;
            this.error = 'You need to be logged in to use this!';
            return false;
        }),
        tap(_ => this.loading = true),
        switchMap(_ => this.api.embeddings().observable),
        catchError((error) => {
            this.error = 'An error occurred while fetching embeddings. Are you logged in?';
            console.error('An error occurred while fetching embeddings', { error });
            return of(undefined)
        }),
        tap(_ => this.loading = false)
    );

    constructor(
        private api: AiService,
        private auth: AuthService,
        private title: Title,
        private route: ActivatedRoute,
        private router: Router
    ) { }

    ngOnInit() {
        this.error = undefined;
        this.title.setTitle('CBA | Image Gen');
        this.id$.subscribe(id => { console.log('Found AI Image', { id }) });
    }

    ngOnDestroy(): void {
        this.title.setTitle(this.api.defaultTitle);
        this._subs.unsubscribe();
    }

    clone<T>(item: T) { return <T>JSON.parse(JSON.stringify(item)); }

    post() {
        this.loading = true;
        this.issues = [];
        this.urls = [];
        this.error = undefined;
        
        let req = (!this.img ? this.api.text2Image : this.api.image2image);

        req.call(this.api, this.request)
            .error(err => {
                let statusCode = err.status;
                if (statusCode != 400) {
                    this.error = err.statusText || 'An error occurred.';
                    return;
                }
                
                this.issues = err.error.issues;
            }, { id: -1 })
            .subscribe(t => {
                this.loading = false;
                if (t.id === -1) return;

                this.router.navigate(['/ai', t.id ]);
            });
    }

    imageUrl(url: string) {
        return `${this.api.apiUrl}/${url}`;
    }
}
