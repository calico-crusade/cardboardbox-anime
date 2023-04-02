import { DbObject } from "../anime/anime.model";

export enum ChatStatus {
    OnGoing = 0,
    ClientFinished = 1,
    ReachedLimit = 2,
    ErrorOccurred = 3
}

export enum ChatMessageType {
    User = 0,
    Bot = 1,
    Image = 2
}

export interface ChatData extends DbObject {
    status: ChatStatus;
    grounder: string;
    profileId: number;
}

export interface ChatMessage extends DbObject {
    chatId: number;
    type: ChatMessageType;
    content: string;
    profileId?: number;
    imageId?: number;
}

export interface Chat {
    chat: ChatData;
    messages: ChatMessage[];
}

export interface ChatResponse {
    code: number;
    worked: boolean;
    message: string;
}