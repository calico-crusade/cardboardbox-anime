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
    ext: {
        type: string;
        value: string;
    }[];

    otherPlatforms: Anime[];

    createdAt: Date;
    updatedAt: Date;
    deletedAt?: Date;
}

export interface PagedResults<T> {
    pages: number;
    count: number;
    results: T[];
}

export type AvailableParams = 'platforms' | 'languages' | 'ratings' | 'types' | 'tags' | 'video types' | 'tag' | 'sorts';

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

export interface PublicLists {
    results: PublicList[];
    total: number;
}

export interface PublicList {
    listId: number;
    listTitle: string;
    listDescription: string;
    listLastUpdate: Date;
    listCount: number;
    listTags: string[];
    listLanguages: string[];
    listLanguageTypes: string[];
    listVideoTypes: string[];
    listPlatforms: string[];
    profileId: number;
    profileUsername: string;
    profileAvatar: string;
}

export interface Chapter extends DbObject {
    hashId: string;
    bookId: string;
    book: string;
    chapter: string;
    content: string;
    url: string;
    nextUrl: string;
    ordinal: number;
}

export interface Book {
    id: string;
    title: string;
    chapters: number;
    updatedAt: Date;
    createdAt: Date;
}