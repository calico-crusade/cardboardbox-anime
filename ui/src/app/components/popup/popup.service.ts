import { Injectable } from "@angular/core";
import { Subject, Subscription } from "rxjs";
import { PopupComponent } from "./popup.component";

export class PopupInstance {
    private _sub: Subject<boolean> = new Subject();
    private _subs: Subscription[] = [];

    get result() { return this._sub.asObservable(); }
    
    constructor(
        private instance: PopupComponent
    ) { 
        this._subs.push(
            this.instance.confirmed.subscribe(() => this.resolve(false)),
            this.instance.canceled.subscribe(() => this.resolve(true))
        );
    }

    private resolve(canceled: boolean) {
        this._sub.next(!canceled);
        this.instance.show = false;

        for(const sub of this._subs)
            sub.unsubscribe();
    }

    cancel() { this.resolve(true); }

    ok() { this.resolve(false); }
}

@Injectable({ providedIn: 'root' })
export class PopupService {
    show(instance: PopupComponent) {
        instance.show = true;
        return new PopupInstance(instance);
    }
}