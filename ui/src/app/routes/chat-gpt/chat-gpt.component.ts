import { AfterViewChecked, Component, ElementRef, OnInit, ViewChild } from '@angular/core';
import { AuthService, ChatService } from 'src/app/services';

@Component({
    selector: 'cba-chat-gpt',
    templateUrl: './chat-gpt.component.html',
    styleUrls: ['./chat-gpt.component.scss']
})
export class ChatGptComponent implements OnInit, AfterViewChecked {
    @ViewChild('scrollMe') scrollMe!: ElementRef;

    loading: boolean = false;

    chat$ = this._chat.messages$;
    message: string = '';

    get user() { return this._auth.currentUser; }

    constructor(
        private _chat: ChatService,
        private _auth: AuthService
    ) { }

    ngOnInit(): void {
        this.scrollToBottom();
    }

    ngAfterViewChecked() {        
        this.scrollToBottom();        
    } 

    send() {
        if (!this.message) return;

        this.loading = true;
        this._chat
            .send(this.message)
            .subscribe(() => {
                this.loading = false;
            });
        this.message = '';
    }

    scrollToBottom() {
        try {
            this.scrollMe.nativeElement.scrollTop = this.scrollMe.nativeElement.scrollHeight;
        } catch(err) { } 
    }
}
