import { Injectable } from "@angular/core";
import { BehaviorSubject } from "rxjs";
import { ConfigObject } from "../config.base";
import { HttpService } from "../http.service";
import { 
    AiDbRequest, AiLoras, AiRequest, 
    AiRequestImg2Img, AiResults, AiSamplers 
} from "./ai.models";

export type IdResponse = { id: number };

@Injectable({
    providedIn: 'root'
})
export class AiService extends ConfigObject {

    private _nextSub = new BehaviorSubject<AiRequest | undefined>(undefined);

    get onGen() { return this._nextSub.asObservable(); }
    get cache() { return this._nextSub.value; }

    constructor(
        private http: HttpService
    ) { super(); }

    text2Image(req: AiRequest) {
        return this.http.post<IdResponse>(`ai`, req, { params: { download: false } });
    }

    image2image(req: AiRequestImg2Img) {
        return this.http.post<IdResponse>(`ai/img`, req, { params: { download: false } });
    }

    embeddings() {
        return this.http.get<string[]>(`ai/embeddings`);
    }

    loras() {
        return this.http.get<AiLoras>(`ai/loras`);
    }

    samplers() {
        return this.http.get<AiSamplers>(`ai/samplers`);
    }

    images() {
        return this.http.get<string[]>(`ai/images`);
    }

    request(id: number) {
        return this.http.get<AiDbRequest>(`ai/${id}`);
    }

    requests(id?: number, page: number = 1, size: number = 100) {
        let res: any = id ? {  id, page, size } : { page, size };

        return this.http.get<AiResults>(`ai`, {
            params: res
        })
    }

    reload(req?: AiDbRequest) {
        if (!req) return;

        let output: AiRequest;

        if (req.denoiseStrength && req.imageUrl) {
            output = <AiRequestImg2Img>{
                prompt: req.prompt,
                negative_prompt: req.negativePrompt,
                steps: req.steps,
                n_iter: req.batchCount,
                batch_size: req.batchSize,
                cfg_scale: req.cfgScale,
                seed: req.seed,
                height: req.height,
                width: req.width,
                init_images: [req.imageUrl],
                denoise_strength: req.denoiseStrength,
                sampler_name: req.sampler
            };
        } else {
            output = {
                prompt: req.prompt,
                negative_prompt: req.negativePrompt,
                steps: req.steps,
                n_iter: req.batchCount,
                batch_size: req.batchSize,
                cfg_scale: req.cfgScale,
                seed: req.seed,
                height: req.height,
                width: req.width,
                sampler_name: req.sampler
            };
        }

        this._nextSub.next(output);
    }

    clear() {
        this._nextSub.next(undefined);
    }

    convertTo(db: AiDbRequest): AiRequestImg2Img {
        return {
            prompt: db.prompt,
            negative_prompt: db.negativePrompt,
            steps: db.steps,
            n_iter: db.batchCount,
            batch_size: db.batchSize,
            cfg_scale: db.cfgScale,
            seed: db.seed,
            height: db.height,
            width: db.width,
            init_images: [db.imageUrl || ''],
            denoise_strength: db.denoiseStrength,
            sampler_name: db.sampler,
        }
    }
}
