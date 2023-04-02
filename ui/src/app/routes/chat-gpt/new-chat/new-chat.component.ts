import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { ChatService } from 'src/app/services';

@Component({
    templateUrl: './new-chat.component.html',
    styleUrls: ['./new-chat.component.scss']
})
export class NewChatComponent {

    content = '';
    error?: string;
    loading = false;


    constructor(
        private api: ChatService,
        private router: Router
    ) { }

    post() {
        this.loading = true;
        this.api
            .newChat(this.content)
            .error(err => {
                this.error = 'An error occurred while requesting a new chat.';
                console.error('An error occurred while requesting a chat', { err });
            }, undefined)
            .subscribe(t => {
                this.loading = false;
                if (!t) return;

                this.router.navigate(['/chat', t.id ]);
            });
    }

}
