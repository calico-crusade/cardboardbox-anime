import { Injectable } from "@angular/core";
import { asyncScheduler, filter, observeOn, switchMap } from "rxjs";
import { AuthService } from "../auth/auth.service";
import { HttpService, RxjsHttpResp } from "../http.service";
import { CachedObservable } from "../utilities.service";
import { Chat, ChatData, ChatResponse } from "./chat.model";

@Injectable({ providedIn: 'root' })
export class ChatService {

    private _chats = new CachedObservable<ChatData[]>(() => 
        this.auth.onLogin.pipe(
            observeOn(asyncScheduler),
            filter(t => {
                if (t) return true;
                return false;
            }),
            switchMap(_ => this.http.get<ChatData[]>(`chat`).observable)
        )
    );

    chats$ = this._chats.data;

    constructor(
        private http: HttpService,
        private auth: AuthService
    ) { }

    newChat(content?: string) {
        return this.http.post<{ id: number }>(`chat`, { content })
            .tap(_ => this._chats.invalidate());
    }

    chat(): RxjsHttpResp<ChatData[]>;
    chat(id: number): RxjsHttpResp<Chat>;
    chat(id: number, prompt: string): RxjsHttpResp<ChatResponse>;
    chat(t1?: number, t2?: string) {
        if (!t1 && !t2) return this.http.get<ChatData[]>(`chat`);

        if (t1 && t2) return this.http.post<ChatResponse>(`chat/${t1}`, { content: t2 });

        return this.http.get<Chat>(`chat/${t1}`);
    }
}