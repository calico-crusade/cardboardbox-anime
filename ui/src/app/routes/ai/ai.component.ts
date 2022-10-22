import { Component } from '@angular/core';
import { catchError, of } from 'rxjs';
import { AiRequestImg2Img, AiService } from 'src/app/services';

@Component({
    templateUrl: './ai.component.html',
    styleUrls: ['./ai.component.scss']
})
export class AiComponent {

    loading: boolean = false;
    issues: string[] = [];
    error?: string;
    urls: string[] = [];

    img: boolean = false;

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
        private api: AiService
    ) { }

    post() {
        this.loading = true;
        this.issues = [];
        this.urls = [];
        this.error = undefined;
        
        let req = (!this.img ? this.api.text2Image : this.api.image2image);

        req.call(this.api, this.request)
            .pipe(
                catchError(err => {
                    const def = of({ urls: [] });
                    console.error('Error occurred in AI request', { 
                        req, 
                        request: this.request, 
                        err 
                    });

                    let statusCode = err.status;
                    if (statusCode != 400) {
                        this.error = err.statusText || 'An error occurred.';
                        return def;
                    }
                    
                    this.issues = err.error.issues;
                    return def;
                })
            )
            .subscribe(t => {
                this.urls = t.urls;
                this.loading = false;
            });
    }

    imageUrl(url: string) {
        return `${this.api.apiUrl}/${url}`;
    }
}
