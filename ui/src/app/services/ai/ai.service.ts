import { HttpClient } from "@angular/common/http";
import { Injectable } from "@angular/core";
import { ConfigObject } from "../config.base";
import { AiRequest, AiRequestImg2Img } from "./ai.models";

export type AiResponse = { urls: string[] };

@Injectable({
    providedIn: 'root'
})
export class AiService extends ConfigObject {
    
    constructor(
        private http: HttpClient
    ) { super(); }

    text2Image(req: AiRequest) {
        return this.http.post<AiResponse>(`${this.apiUrl}/ai`, req, { params: { download: false } });
    }

    image2image(req: AiRequestImg2Img) {
        return this.http.post<AiResponse>(`${this.apiUrl}/ai/img`, req, { params: { download: false } });
    }

    embeddings() {
        return this.http.get<string[]>(`${this.apiUrl}/ai/embeddings`);
    }

    images() {
        return this.http.get<string[]>(`${this.apiUrl}/ai/images`);
    }
}