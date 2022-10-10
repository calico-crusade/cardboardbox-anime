import { Injectable } from '@angular/core';
import { ActivatedRouteSnapshot, CanActivate, CanActivateChild, Router, RouterStateSnapshot } from '@angular/router';
import { AuthService } from './auth.service';

@Injectable({
    providedIn: 'root'
})
export class AdminGuard implements CanActivate, CanActivateChild {

    constructor(
        private auth: AuthService,
        private router: Router
    ) { }

    can() {
        if (this.isAdmin()) return true;

        this.router.navigate(['/anime']);
        return false;
    }

    isAdmin() {
        const roles = this.auth.currentUser?.roles || [];
        return roles.indexOf('Admin') !== -1;
    }

    canActivate(route: ActivatedRouteSnapshot, state: RouterStateSnapshot) {
        return this.can();
    }

    canActivateChild(childRoute: ActivatedRouteSnapshot, state: RouterStateSnapshot) {
        return this.can();
    }

}
