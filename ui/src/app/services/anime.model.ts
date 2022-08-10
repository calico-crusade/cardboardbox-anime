export interface Image {
    width?: number;
    height?: number;
    type: string;
    source: string;
    platformId: string;
}

export interface Anime {
    id: number;
    hashId: string;
    animeId: string;
    link: string;
    title: string;
    description: string;
    type: string;
    platformId: string;
    languages: string[];
    languageTypes: string[];
    ratings: string[];
    tags: string[];
    mature: boolean;
    
    images: Image[];

    createdAt: Date;
    updatedAt: Date;
    deletedAt?: Date;
}

export interface PagedResults {
    pages: number;
    count: number;
    results: Anime[];
}

export type AvailableParams = 'platforms' | 'languages' | 'ratings' | 'types' | 'tags' | 'video types';

export interface Filter {
    key: AvailableParams;
    values: string[];
}

export type Filters = Filter[];

export enum MatureType {
    Both = 0,
    Mature = 1,
    Everyone = 2
}

export interface FilterSearch {
    page: number;
    size: number;
    search?: string;
    queryables: {
        [key: string]: string[];
    }
    asc: boolean;
    mature: MatureType;
    listId?: number;
}

export interface Id { id: number ;}

export interface DbObject {
    id: number;

    createdAt: Date;
    updatedAt: Date;
    deletedAt?: Date;
}

export interface ListPost {
    title: string;
    description: string;
}

export interface ListPut extends ListPost {
    id: number;
    isPublic: boolean;
}

export interface List extends ListPut, DbObject {
    profileId: number;
}

export interface ListExt extends List {
    count: number;
}

export interface ListMap {
    animeId: number;
    listId: number;
}

export interface ListMapItem {
    listId: number;
    animeIds: number[];
}