import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService, SubscriptionHandler } from './../../services';

@Component({
    templateUrl: './callback.component.html',
    styleUrls: ['./callback.component.scss']
})
export class CallbackComponent implements OnInit, OnDestroy {

    private _subs = new SubscriptionHandler();

    constructor(
        private route: ActivatedRoute,
        private router: Router,
        private auth: AuthService
    ) { }

    ngOnInit(): void {
        this._subs
            .subscribe(this.route.queryParams, (t) => {
                let { code, redirect } = t;
                this.handleCode(code, redirect);
            });
    }

    ngOnDestroy(): void {
        this._subs.unsubscribe();
    }

    async handleCode(code: string, redirect: string) {
        try {
            const auth = await this.auth.handleCode(code);
            if (auth.error) {
                //Error occurred
                console.error('Error occurred during login', { auth });
                this.router.navigate(['/error'], { queryParams: { error: auth.error }});
                return;
            }

            this.router.navigateByUrl(redirect);
        } catch (err) {
            console.error('Error occurred during login check', { err, code, redirect });
            this.router.navigate(['/error'], { queryParams: { error: err }});
        }
    }
}
