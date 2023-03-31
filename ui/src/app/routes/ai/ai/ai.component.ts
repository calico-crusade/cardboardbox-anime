import { Component, OnDestroy, OnInit } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { ActivatedRoute, Router } from '@angular/router';
import { asyncScheduler, catchError, filter, map, observeOn, of, switchMap, tap } from 'rxjs';
import { AiRequestImg2Img, AiService, AuthService, SubscriptionHandler } from 'src/app/services';

const DEFAULT_REQUEST : AiRequestImg2Img = {
    prompt: 'a girl,Phoenix girl,fluffy hair,war,a hell on earth,Beautiful and detailed explosion,Cold machine,Fire in eyes,World War,burning,Metal texture,Exquisite cloth,Metal carving,volume,best quality,normal hands,Metal details,Metal scratch,Metal defects,{{masterpiece}},best quality,official art,4k,best quality,extremely detailed CG unity 8k,illustration,highres,masterpiece,contour deepening,Azur Lane,Girls Front,Magical,Cloud Map Plan,contour deepening,long-focus,Depth of field,a cloudy sky,Black smoke,smoke of gunpowder,long-focus,Mature,resolute eyes,burning,burning sky,burning hair,Burn oneself in flames,fighting,Covered in blood,complex pattern,battleing,Flying flames,Flame whirlpool,Doomsday Scenes,float,Splashing blood,on the battlefield,Bloody scenes,Good looking flame,Exquisite Flame,Exquisite Blood,photorealistic,Watercolor,colourful,(((masterpiece))),best quality,illustration,beautiful detailed glow,detailed ice,beautiful detailed water,red moon,(magic circle:1,2),(beautiful detailed eyes),expressionless,beautiful detailed white gloves,own hands clasped,(floating palaces:1.1),azure hair,disheveled hair,long bangs,hairs between eyes,dark dress,(dark magician girl:1.1),black kneehighs,black ribbon,white bowties,midriff,{{{half closed eyes}}},,big forhead,blank stare,flower,large top sleeves,,(((masterpiece))),best quality,illustration,(beautiful detailed girl),beautiful detailed glow,detailed ice,beautiful detailed water,(beautiful detailed eyes),expressionless,beautiful detailed white gloves,(floating palaces:1.2),azure hair,disheveled hair,long bangs,hairs between eyes,(skyblue dress),black ribbon,white bowties,midriff,{{{half closed eyes}}},,big forhead,blank stare,flower,large top sleeves,(((ice crystal texture wings)),(((ice and fire melt)))',
    negativePrompt: '(((ugly))),(((duplicate))),((morbid)),((mutilated)),(((tranny))),mutated hands,(((poorly drawn hands))),blurry,((bad anatomy)),(((bad proportions))),extra limbs,cloned face,(((disfigured))),(((more than 2 nipples))),((((missing arms)))),(((extra legs))),mutated hands,(((((fused fingers))))),(((((too many fingers))))),(((unclear eyes))),lowers,bad anatomy,bad hands,text,error,missing fingers,extra digit,fewer digits,cropped,worst quality,low quality,normal quality,jpeg artifacts,signature,watermark,username,blurry,bad feet,text font ui,malformed hands,long neck,missing limb,(mutated hand and finger: 1.5),(long body: 1.3),(mutation poorly drawn: 1.2),disfigured,malformed mutated,multiple breasts,futa,yaoi',
    steps: 39,
    batchCount: 1,
    batchSize: 1,
    cfgScale: 4.5,
    seed: -1,
    height: 1024,
    width: 512,
    image: '',
    denoiseStrength: 0.2
}

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
