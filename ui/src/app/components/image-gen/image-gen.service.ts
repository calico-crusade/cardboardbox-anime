import { Injectable } from "@angular/core";
import { BehaviorSubject } from "rxjs";

export enum GenEnum {
    Hidden = 0,
    Shown = 1,
    Loading = 2,
    Results = 3,
    Error = 4
}

@Injectable({ providedIn: 'root' })
export class ImageGenService {
    private _stateSub = new BehaviorSubject<GenEnum>(GenEnum.Hidden);
    private _triggerSub = new BehaviorSubject<string | undefined>(undefined);
    private _genSub = new BehaviorSubject<number | undefined>(undefined);

    get onState() { return this._stateSub.asObservable(); }
    get onPrompt() { return this._triggerSub.asObservable(); }
    get onGenerated() { return this._genSub.asObservable(); }

    open(prompt: string) {
        this._triggerSub.next(prompt);
        this._stateSub.next(GenEnum.Shown);
    }

    close() {
        this._stateSub.next(GenEnum.Hidden);
    }

    generated(id: number) {
        this._genSub.next(id);
        this._stateSub.next(GenEnum.Results);
    }
}