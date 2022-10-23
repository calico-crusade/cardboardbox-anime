import { HttpClient } from "@angular/common/http";
import { Injectable } from "@angular/core";
import { BehaviorSubject } from "rxjs";
import { ConfigObject } from "../config.base";
import { AiDbRequest, AiRequest, AiRequestImg2Img, AiResults } from "./ai.models";

export type UrlsResponse = { urls: string[] };

@Injectable({
    providedIn: 'root'
})
export class AiService extends ConfigObject {
    
    private _nextSub = new BehaviorSubject<AiRequest | undefined>(undefined);

    get onGen() { return this._nextSub.asObservable(); }
    get cache() { return this._nextSub.value; }

    constructor(
        private http: HttpClient
    ) { super(); }

    text2Image(req: AiRequest) {
        return this.http.post<UrlsResponse>(`${this.apiUrl}/ai`, req, { params: { download: false } });
    }

    image2image(req: AiRequestImg2Img) {
        return this.http.post<UrlsResponse>(`${this.apiUrl}/ai/img`, req, { params: { download: false } });
    }

    embeddings() {
        return this.http.get<string[]>(`${this.apiUrl}/ai/embeddings`);
    }

    images() {
        return this.http.get<string[]>(`${this.apiUrl}/ai/images`);
    }

    requests(id?: number, page: number = 1, size: number = 100) {
        let res: any = id ? {  id, page, size } : { page, size };

        return this.http.get<AiResults>(`${this.apiUrl}/ai/requests`, {
            params: res
        })
    }

    reload(req?: AiDbRequest) {
        if (!req) return;

        let output: AiRequest;

        if (req.denoiseStrength && req.imageUrl) {
            output = <AiRequestImg2Img>{
                prompt: req.prompt,
                negativePrompt: req.negativePrompt,
                steps: req.steps,
                batchCount: req.batchCount,
                batchSize: req.batchSize,
                cfgScale: req.cfgScale,
                seed: req.seed,
                height: req.height,
                width: req.width,
                image: req.imageUrl,
                denoiseStrength: req.denoiseStrength
            };
        } else {
            output = {
                prompt: req.prompt,
                negativePrompt: req.negativePrompt,
                steps: req.steps,
                batchCount: req.batchCount,
                batchSize: req.batchSize,
                cfgScale: req.cfgScale,
                seed: req.seed,
                height: req.height,
                width: req.width
            };
        }

        this._nextSub.next(output);
    }

    clear() {
        this._nextSub.next(undefined);
    }
}