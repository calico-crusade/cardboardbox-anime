

export interface ChatMessage {
    role: 'system' | 'user' | 'assistant';
    content: string;
}

export interface ChatResponse {
    message: ChatMessage;
    usage: {
        prompt_tokens: number;
        completion_tokens: number;
        total_tokens: number;
    }    
}