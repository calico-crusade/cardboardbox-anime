import { Component, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { ActivatedRoute, Router } from '@angular/router';
import { asyncScheduler, catchError, combineLatest, combineLatestWith, filter, map, observeOn, of, switchMap, tap } from 'rxjs';
import { PopupComponent, PopupInstance, PopupService } from 'src/app/components';
import { AiRequestImg2Img, AiService, AuthService, SubscriptionHandler, DEFAULT_REQUEST, AuthUser, AiLoras, AiSamplers } from 'src/app/services';

@Component({
    templateUrl: './ai.component.html',
    styleUrls: ['./ai.component.scss']
})
export class AiComponent implements OnInit, OnDestroy {

    private _subs = new SubscriptionHandler();
    private _embedsPopupIns?: PopupInstance;
    private _loraPopupIns?: PopupInstance;

    @ViewChild('embedsPopup') embedsPopup!: PopupComponent;
    @ViewChild('loraPopup') loraPopup!: PopupComponent;

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
        tap(tRaw => {
            this.loading = false;
            if (!tRaw) return;

            const t = this.api.convertTo(tRaw);

            this.request = t;
            this.img = !!t.init_images[0];
            this.request.init_images = t.init_images;
            this.request.denoise_strength = t.denoise_strength || 0.2;
            this.urls = tRaw.outputPaths;
        }),
        map(t => t?.id)
    );

    dataSet$ = this.auth.onLogin.pipe(
        observeOn(asyncScheduler),
        filter(authUser => {
            this.error = undefined;
            if (authUser) return true;
            this.error = 'You need to be logged in to use this!';
            return false;
        }),
        tap(_ => this.loading = true),
        combineLatestWith(
            this.api.embeddings().observable,
            this.api.loras().observable,
            this.api.samplers().observable,
        ),
        catchError((error) => {
            this.error = 'An error occurred while fetching embeddings and/or loras. Are you logged in?';
            console.error('An error occurred while fetching embeddings and/or loras', { error });
            return of([undefined, undefined, undefined, undefined])
        }),
        tap(_ => this.loading = false)
    ).subscribe(([_, embeddings, loras, samplers]) => {
        this.embeddings = embeddings || [];
        this.loras = loras || [];
        this.samplers = samplers || [];
    });

    embeddings: string[] = [];
    loras: AiLoras = [];
    samplers: AiSamplers = [];

    constructor(
        private api: AiService,
        private auth: AuthService,
        private title: Title,
        private route: ActivatedRoute,
        private router: Router,
        private pop: PopupService
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

    openEmbeds() {
        this._embedsPopupIns = this.pop.show(this.embedsPopup);
    }

    openLoras() {
      this._loraPopupIns = this.pop.show(this.loraPopup);
    }

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
