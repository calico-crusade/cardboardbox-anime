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