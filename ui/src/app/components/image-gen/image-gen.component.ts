import { Component, OnInit, ViewChild } from '@angular/core';
import { AiService, DEFAULT_REQUEST } from 'src/app/services';
import { PopupComponent } from '../popup/popup.component';
import { PopupInstance, PopupService } from '../popup/popup.service';
import { GenEnum, ImageGenService } from './image-gen.service';

@Component({
    selector: 'cba-image-gen',
    templateUrl: './image-gen.component.html',
    styleUrls: ['./image-gen.component.scss']
})
export class ImageGenComponent implements OnInit {

    private _instance?: PopupInstance;

    @ViewChild('popup') popup!: PopupComponent;

    loading = false;
    error?: string;
    request = this.clone(DEFAULT_REQUEST);
    img = false;
    hints = false;
    advanced = false;
    issues: string[] = [];
    

    constructor(
        private _popSrv: PopupService,
        private _imgSrv: ImageGenService,
        private _api: AiService
    ) { }

    ngOnInit(): void {
        this._imgSrv.onState.subscribe(t => {
            switch(t) {
                case GenEnum.Hidden: this._instance?.cancel(); break;
                case GenEnum.Shown: this._instance = this._popSrv.show(this.popup); break;
                case GenEnum.Loading: this.loading = true; break;
                case GenEnum.Results: this.loading = false; this._instance?.cancel(); break;
                case GenEnum.Error: this.loading = false; break;
            }
        });

        this._imgSrv.onPrompt.subscribe(t => {
            this.request.prompt = t || DEFAULT_REQUEST.prompt;
        });
    }

    clone<T>(item: T) { return <T>JSON.parse(JSON.stringify(item)); }

    open() {
        this._instance = this._popSrv.show(this.popup);
    }

    generate() {
        this.loading = true;
        this.issues = [];
        this.error = undefined;
        
        let req = (!this.img ? this._api.text2Image : this._api.image2image);

        req.call(this._api, this.request)
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

                this._imgSrv.generated(t.id);
                // this.router.navigate(['/ai', t.id ]);
            });
    }

    clear() {
        this.error = undefined;
        this.issues = [];
    }
}
