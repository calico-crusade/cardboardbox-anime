import { Component, OnDestroy, OnInit } from '@angular/core';
import { Location } from '@angular/common';
import { ActivatedRoute, NavigationEnd, Router } from '@angular/router';
import { CapacitorStorageVar, LightNovelService, MangaProgressData, MangaService, SubscriptionHandler } from '../services';
import { AuthUser } from '../services/auth/auth.model';
import { AuthService } from '../services/auth/auth.service';
import { Capacitor } from '@capacitor/core';

@Component({
    selector: 'cba-root',
    templateUrl: './app.component.html',
    styleUrls: ['./app.component.scss']
})
export class AppComponent implements OnInit, OnDestroy {

    private _subs = new SubscriptionHandler();

    user?: AuthUser;
    menuOpen = false;
    menuPin = false;
    title?: string;
    showTitle: boolean = true;
    updated: MangaProgressData[] = [];

    get isAdmin() {
        if (!this.user) return false;
        const roles = this.user.roles || [];
        return roles.indexOf('Admin') !== -1;
    }

    get loggedIn() { return this.user; }

    constructor(
        private auth: AuthService,
        private router: Router,
        private manga: MangaService,
        private ln: LightNovelService,
        private loc: Location
    ) { }

    async ngOnInit() {
        await CapacitorStorageVar.init();
        this._subs
            .subscribe(this.router.events, t => {
                if (t instanceof NavigationEnd) {
                    const url = t.url.substring(1);
                    this.auth.lastRoute = url;
                    this.bumpManga();
                }
            })
            .subscribe(this.auth.onHeaderChange, t => this.showTitle = t)
            .subscribe(this.auth.onLogin, t => this.user = t)
            .subscribe(this.auth.onTitleChange, t => this.title = t);
        await this.auth.bump();
    }

    ngOnDestroy(): void { this._subs.unsubscribe(); }

    async login() {
        if (this.auth.platform !== 'web') {
            this.auth.loginSame(this.router.url);
            return;
        }

        await this.auth.login();
        this.menuOpen = false;
    }

    logout() { 
        this.auth.logout();
        this.menuOpen = false;
    }
    
    toggleDropdown(el: HTMLElement) {
        el.classList.toggle('active');
    }

    proxy(url?: string) {
        return url ? this.ln.corsFallback(url, 'manga-covers') : '';
    }

    async bumpManga() {
        const { results } = await this.manga.promptCheck().promise;
        if (results.length === 0) return;
        this.updated = results;
    }

    back() { return this.loc.back(); }
}
