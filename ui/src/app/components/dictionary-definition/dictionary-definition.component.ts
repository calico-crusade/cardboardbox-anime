import { Component, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { Subscription } from 'rxjs';
import { Definition } from 'src/app/services';
import { PopupComponent } from '../popup/popup.component';
import { PopupInstance, PopupService } from '../popup/popup.service';
import { DictionaryDefinitionService } from './dictionary-definition.service';

@Component({
    selector: 'cba-dictionary-definition',
    templateUrl: './dictionary-definition.component.html',
    styleUrls: ['./dictionary-definition.component.scss']
})
export class DictionaryDefinitionComponent implements OnInit, OnDestroy {

    @ViewChild('popup') popup!: PopupComponent;

    private subs: Subscription[] = [];
    private popIn?: PopupInstance;

    loading: boolean = false;
    text?: string;
    definition: Definition[] = [];

    get audio() {
        for(let def of this.definition) {
            for(let phon of def.phonetics) {
                if (phon.audio) return phon.audio;
            }
        }
        return undefined;
    }
    
    constructor(
        private dic: DictionaryDefinitionService,
        private pop: PopupService
    ) { }

    ngOnInit(): void {
        this.subs = [
            this.dic.onClose.subscribe(t => {
                this.popIn?.ok();
                this.popIn = undefined;
            }),

            this.dic.onLoading.subscribe(t => {
                this.loading = t;
                if (t) this.popIn = this.pop.show(this.popup);
            }),

            this.dic.onText.subscribe(t => this.text = t),
            this.dic.onDefinition.subscribe(t => this.definition = t)
        ];
    }

    ngOnDestroy(): void {
        for(let sub of [...this.subs]) sub.unsubscribe();
        this.subs = [];
    }

    go(text: string) {
        this.popIn?.ok();
        this.dic.definition(text);
    }

    play() {
        const url = this.audio;
        if (!url) return;

        new Audio(url).play();
    }
}
