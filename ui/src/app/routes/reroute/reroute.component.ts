import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService, CapacitorStorageVar } from 'src/app/services';

@Component({
    templateUrl: './reroute.component.html',
    styleUrls: ['./reroute.component.scss']
})
export class RerouteComponent implements OnInit {

    constructor(
        private router: Router,
        private auth: AuthService
    ) { }

    async ngOnInit() {
        await CapacitorStorageVar.init();

        const route = this.auth.lastRoute;
        if (!route) {
            this.router.navigate(['/anime', 'all' ]);
            return;
        }

        const parts = route.split('/');
        this.router.navigate(parts);
    }

}
