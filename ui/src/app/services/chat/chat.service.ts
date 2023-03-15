import { Injectable } from "@angular/core";
import { BehaviorSubject } from "rxjs";
import { environment } from "src/environments/environment";
import { HttpService } from "../http.service";
import { ChatMessage, ChatResponse } from "./chat.model";

@Injectable({ providedIn: 'root' })
export class ChatService {

    private _msgSub = new BehaviorSubject<ChatMessage[]>([]);

    messages$ = this._msgSub.asObservable();

    get messages() { return this._msgSub.getValue(); }

    constructor(
        private http: HttpService
    ) { }

    send(message: string) {
        
        this._msgSub.next([...this.messages, {
            role: 'user',
            content: message
        }]);

        return this.http
            .post<ChatResponse>(`chat`, this.messages)
            .tap((t) => {
                this._msgSub.next([...this.messages, t.message]);
                if (!environment.production)
                    console.log('GPT Usage Report', { response: t });
            });
    }
}