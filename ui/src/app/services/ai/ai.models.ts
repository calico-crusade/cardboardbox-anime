import { DbObject, PagedResults } from "../anime.model";

export interface AiRequest {
    prompt: string;
    negativePrompt: string;
    steps: number;
    batchCount: number;
    batchSize: number;
    cfgScale: number;
    seed: number;
    height: number;
    width: number;
}

export interface AiRequestImg2Img extends AiRequest {
    image: string;
    denoiseStrength: number;
}

export interface AiDbRequest extends AiRequest, DbObject {
    profileId: number;

    imageUrl?: string;
    denoiseStrength?: number;

    outputPaths: string[];
    generationStart: Date;
    generationEnd?: Date;
    secondsElapsed?: number;
}

export type AiResults = PagedResults<AiDbRequest>;