import { AfterViewInit, Component, ElementRef, OnInit, ViewChild } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { asyncScheduler, catchError, filter, map, observeOn, of, switchMap, tap } from 'rxjs';
import { Selection } from 'src/app/components';
import { DictionaryDefinitionService } from 'src/app/components/dictionary-definition/dictionary-definition.service';
import { ImageGenService } from 'src/app/components/image-gen/image-gen.service';
import { AuthService, ChatMessage, ChatMessageType, ChatService } from 'src/app/services';

type TextSelect = Selection & { index: number };

@Component({
    templateUrl: './active-chat.component.html',
    styleUrls: ['./active-chat.component.scss']
})
export class ActiveChatComponent implements OnInit, AfterViewInit {

    @ViewChild('scrollMe') scrollMe!: ElementRef;

    loading = false;
    msgLoading = false;
    msgError?: string;
    error?: string;

    content: string = '';
    messages: ChatMessage[] = [];
    id?: number;
    select?: TextSelect;

    get user() { return this.auth.currentUser; }

    id$ = this.route.params.pipe(map(t => t['id'] ? +t['id'] : undefined));

    data$ = this.auth.onLogin.pipe(
        observeOn(asyncScheduler),
        tap(_ => { this.loading = true; this.error = undefined; }),
        filter(t => !!t),
        switchMap(_ => this.id$),
        filter(t => !!t),
        switchMap(t => this.chat.chat(t || 1).observable),
        catchError(error => {
            console.error('Error occurred while fetching chat data', { error });
            this.error = 'An error occurred! Please try again later or contact an adminstrator';
            return of(undefined)
        }),
        tap(_ => this.loading = false)
    );

    constructor(
        private auth: AuthService,
        private chat: ChatService,
        private route: ActivatedRoute,
        private dic: DictionaryDefinitionService,
        private ai: ImageGenService
    ) { }

    ngOnInit(): void {
        this.data$.subscribe(t => {
            this.messages = t?.messages || [];
            this.scrollToBottom();
        });
        this.id$.subscribe(t => this.id = t);
    }

    ngAfterViewInit(): void {
        this.scrollToBottom();
    }

    send() {
        if (!this.id) return;

        this.msgError = undefined;
        this.addMessage(ChatMessageType.User, this.content);
        
        this.msgLoading = true;
        this.chat
            .chat(this.id, this.content)
            .error(error => {
                this.msgError = 'An error occurred. Please try again later or contact an administrator.';
                console.error('An error occurred.', { error });
            }, undefined)
            .subscribe(t => {
                this.msgLoading = false;
                if (!t) return;

                if (!t.worked) {
                    this.msgError = t.message;
                    console.error('A handled error occurred.', { msg: t });
                    return;
                }

                this.addMessage(ChatMessageType.Bot, t.message);
            });

        this.content = '';
    }

    addMessage(type: ChatMessageType, content: string) {
        this.messages.push({
            id: -1,
            createdAt: new Date(),
            type,
            content,
            chatId: this.id || 0,
            updatedAt: new Date()
        });
        this.scrollToBottom();
    }

    scrollToBottom() {
        setTimeout(() => {
            try {
                this.scrollMe.nativeElement.scrollTop = this.scrollMe.nativeElement.scrollHeight;
            } catch(err) { } 
        }, 100);
    }

    textHighlighted(text: Selection | undefined, index: number) {
        if (!text) this.select = undefined;
        else this.select = { ...text, index };
    }

    dicLookup() {
        if (!this.select || !this.select.text) return;

        this.dic.definition(this.select.text);
    }

    genImage() {
        if (!this.select || !this.select.text) return;
        this.ai.open(this.select.text);
    }
}
