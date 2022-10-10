import { DbObject, PagedResults } from "../anime.model";

export type SeriesResult = PagedResults<NovelSeries>;
export type PagesResult = PagedResults<NovelPage>;

interface HashObject extends DbObject {
    hashId: string;
    title: string;
}

interface MetaObject {
    authors: string[];
    illustrators: string[];
    editors: string[];
    translators: string[];
}

export interface Scaffold {
    series: NovelSeries;
    books: {
        book: NovelBook;
        chapters: NovelChapter[];
    }[];
}

export interface NovelPage extends HashObject {
    ordinal: number;
    seriesId: number;
    url: string;
    nextUrl?: string;
    content: string;
    mimetype: string;
}

export interface NovelChapter extends HashObject {
    ordinal: number;
    bookId: number;
}

export interface NovelBook extends HashObject, MetaObject {
    ordinal: number;
    seriesId: number;
    coverImage?: string;
    forwards: string[];
    inserts: string[];
}

export interface NovelSeries extends HashObject, MetaObject {
    url?: string;
    lastChapterUrl?: string;
    description?: string;
    image?: string;
    genre: string[];
    tags: string[];
}