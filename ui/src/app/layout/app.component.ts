import { Component, OnInit } from '@angular/core';
import { AuthUser } from '../services/auth.model';
import { AuthService } from '../services/auth.service';

@Component({
    selector: 'cba-root',
    templateUrl: './app.component.html',
    styleUrls: ['./app.component.scss']
})
export class AppComponent implements OnInit {

    user?: AuthUser;
    menuOpen = false;

    get loggedIn() { return this.user; }

    constructor(
        private auth: AuthService
    ) { }

    async ngOnInit() {
        this.auth.onLogin.subscribe(t => this.user = t);
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
