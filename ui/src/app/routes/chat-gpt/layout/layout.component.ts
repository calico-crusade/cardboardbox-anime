import { Component, OnInit } from '@angular/core';
import { AuthService, ChatService } from 'src/app/services';

@Component({
    templateUrl: './layout.component.html',
    styleUrls: ['./layout.component.scss']
})
export class LayoutComponent {

    loading: boolean = false;
    error?: string;

    get user() { return this.auth.currentUser; }

    chats$ = this.api.chats$;

    repeater = Array(100);

    constructor(
        private api: ChatService,
        private auth: AuthService
    ) { }
}
