export interface Manga {
    id: number;
    createdAt: Date;
    updatedAt: Date;
    deletedAt?: Date;

    title: string;
    sourceId: string;
    provider: string;
    url: string;
    cover: string;
    tags: string[];
    altTitles: string[];
    description: string;
}

export interface MangaChapter {
    id: number;
    createdAt: Date;
    updatedAt: Date;
    deletedAt?: Date;

    mangaId: number;
    title: string;
    url: string;
    sourceId: string;
    ordinal: number;
    language: string;
    pages: string[];
}

export interface MangaWithChapters {
    manga: Manga;
    chapters: MangaChapter[];
}

export interface PaginatedManga {
    pages: number;
    count: number;
    results: Manga[];
}