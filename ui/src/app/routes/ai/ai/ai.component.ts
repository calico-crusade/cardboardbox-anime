import { Component, OnDestroy, OnInit } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { AiRequestImg2Img, AiService, AuthService, SubscriptionHandler } from 'src/app/services';

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

    embeddings: string[] = [];

    img: boolean = false;
    hints: boolean = false;

    request: AiRequestImg2Img = {
        prompt: 'detailed blushing [anime girl:botan-50000:.05] in a black outfit with ((starry eyes)), a perfect face, lit by ((strong rim light)) at ((night)), (intense shadows), ((sharp focus))',
        negativePrompt: '((extra fingers)) ((poorly drawn hands)) ((poorly drawn face)) (((mutation))) (((deformed))) ((bad anatomy)) (((bad proportions))) ((extra limbs)) glitchy ((extra hands)) ((mangled fingers)) (portrait) (text)(words)(copyright), ((dick)), ((hands)), ((long nails))',
        steps: 12,
        batchCount: 1,
        batchSize: 1,
        cfgScale: 8,
        seed: -1,
        height: 512,
        width: 512,
        image: '',
        denoiseStrength: 0.2
    };

    constructor(
        private api: AiService,
        private auth: AuthService,
        private title: Title
    ) { }

    ngOnInit() {
        this.title.setTitle('CBA | Image Gen')

        this._subs
            .subscribe(this.auth.onLogin, t => {
                this.process();
            });

        const cache = this.api.cache;
        if (cache) {
            this.request = <AiRequestImg2Img>cache;
            this.img = !!this.request.image;
            this.request.image = this.request.image || '';
            this.request.denoiseStrength = this.request.denoiseStrength || 0.2;
        }

        this.process();
    }

    ngOnDestroy(): void {
        this.title.setTitle(this.api.defaultTitle);
        this._subs.unsubscribe();
    }

    private process() {
        this.error = undefined;
        this.api
            .embeddings()
            .error(err => {
                if (err.status !== 401) {
                    this.error = err.statusText;
                }
            })
            .subscribe(t => {
                this.embeddings = t;
            });
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
            }, { urls: [] })
            .subscribe(t => {
                this.urls = t.urls;
                this.loading = false;
            });
    }

    imageUrl(url: string) {
        return `${this.api.apiUrl}/${url}`;
    }
}
