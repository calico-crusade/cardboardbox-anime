import { Injectable } from "@angular/core";
import { BehaviorSubject } from "rxjs";
import { Definition, DictionaryService } from "src/app/services";

@Injectable({ providedIn: 'root' })
export class DictionaryDefinitionService {

    private _onLoadingSub = new BehaviorSubject<boolean>(false);
    private _onDefinitionSub = new BehaviorSubject<Definition[]>([]);
    private _onCloseSub = new BehaviorSubject<boolean>(false);
    private _onTextSub = new BehaviorSubject<string | undefined>(undefined);

    get onLoading() { return this._onLoadingSub.asObservable(); }
    get onDefinition() { return this._onDefinitionSub.asObservable(); }
    get onClose() { return this._onCloseSub.asObservable(); }
    get onText() { return this._onTextSub.asObservable(); }

    constructor(
        private dic: DictionaryService
    ) {
        this.onClose.subscribe(t => {
            this._onDefinitionSub.next([]);
            this._onTextSub.next(undefined);
            this._onLoadingSub.next(false);
        });
    }

    close() { this._onCloseSub.next(true); }

    definition(text: string) {
        this._onTextSub.next(text);
        this._onLoadingSub.next(true);

        this.dic.get(text)
            .subscribe(t => {
                this._onDefinitionSub.next(t);
                this._onLoadingSub.next(false);
            });
    }
}