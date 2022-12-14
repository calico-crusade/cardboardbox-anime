import { Component, EventEmitter, Input, OnDestroy, OnInit, Output } from '@angular/core';
import { IInfiniteScrollEvent } from 'ngx-infinite-scroll';
import { AuthService, SubscriptionHandler } from 'src/app/services';

type state = 'loading' | 'error' |
    'requires-login-loading' |
    'requires-login-not-admin' |
    'requires-login-not-loggedin' | 
    'none';

@Component({
    selector: 'cba-container',
    templateUrl: './component-container.component.html',
    styleUrls: ['./component-container.component.scss']
})
export class ComponentContainerComponent implements OnInit, OnDestroy {

    private _subs = new SubscriptionHandler();

    private _loading: boolean = false;
    private _error?: string;
    private _reqLogin: boolean = false;
    private _reqAdmin: boolean = false;

    @Input()
    set loading(value: boolean) { 
        this._loading = value; 
        this.process(); 
    }

    @Input() 
    set error(value: string | undefined) { 
        this._error = value; 
        this.process(); 
    }

    @Input('requires-login') 
    set reqLogin(value: boolean) { 
        this._reqLogin = value; 
        this.process(); 
    }

    @Input('requires-admin') 
    set reqAdmin(value: boolean) { 
        this._reqAdmin = value; 
        this.process(); 
    }

    @Input('handle-scroll') handleScroll: boolean = false;
    @Input('flex-flow') flow: string = 'row';

    @Input('ifs-distance') ifsDistance: number = 2;
    @Input('ifs-throlle') ifsThrottle: number = 50;

    @Input('ifs-enabled') ifsEnabled: boolean = false;

    @Output('ifs') onScrolled = new EventEmitter<IInfiniteScrollEvent>(); 

    state: state = 'loading';

    get isAdmin() {
        let roles = this.auth.currentUser?.roles || [];
        return roles.indexOf('Admin') !== -1;
    }

    constructor(
        private auth: AuthService
    ) { }

    ngOnInit(): void {
        this._subs
            .subscribe(this.auth.onLogin, _ => this.process())
            .subscribe(this.auth.onIsLoggingIn, _ => this.process());
    }

    ngOnDestroy(): void { this._subs.unsubscribe(); }

    process() {
        if (this._loading) {
            this.state = 'loading';
            return;
        }

        if (this._error) {
            this.state = 'error';
            return;
        }

        if (!this._reqAdmin && !this._reqLogin) {
            this.state = 'none';
            return;
        }

        if (this.auth.isLoggingIn) {
            this.state = 'requires-login-loading';
            return;
        }

        if (!this.auth.currentUser) {
            this.state = 'requires-login-not-loggedin';
            return;
        }

        if (this._reqAdmin && !this.isAdmin) {
            this.state = 'requires-login-not-admin';
            return;
        }
        
        this.state = 'none';
    }

    onScrollTriggered(event: IInfiniteScrollEvent) {
        if (!this.ifsEnabled) return;

        this.onScrolled.emit(event);
    }
}
