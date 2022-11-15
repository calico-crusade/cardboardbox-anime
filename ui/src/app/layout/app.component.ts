import { Component, OnInit } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { AuthUser } from '../services/auth/auth.model';
import { AuthService } from '../services/auth/auth.service';

@Component({
    selector: 'cba-root',
    templateUrl: './app.component.html',
    styleUrls: ['./app.component.scss']
})
export class AppComponent implements OnInit {

    user?: AuthUser;
    menuOpen = false;
    title?: string;
    showTitle: boolean = true;

    get isAdmin() {
        if (!this.user) return false;
        const roles = this.user.roles || [];
        return roles.indexOf('Admin') !== -1;
    }

    get loggedIn() { return this.user; }

    constructor(
        private auth: AuthService,
        private router: Router
    ) { }

    async ngOnInit() {
        this.router
            .events
            .subscribe(t => {
                if (t instanceof NavigationEnd) {
                    const url = t.url.substring(1);
                    this.auth.lastRoute = url;
                }
            });

        this.auth.onHeaderChange.subscribe(t => this.showTitle = t);
        this.auth.onLogin.subscribe(t => this.user = t);
        this.auth.onTitleChange.subscribe(t => this.title = t);
        await this.auth.bump();
    }

    async login() {
        await this.auth.login();
        this.menuOpen = false;
    }

    logout() { 
        this.auth.logout();
        this.menuOpen = false;
    }
}
