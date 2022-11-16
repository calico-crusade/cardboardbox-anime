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
    volume?: number;
    language: string;
    pages: string[];
}

export interface MangaBookmark {
    id: number;
    createdAt: Date;
    updatedAt: Date;
    deletedAt?: Date;

    profileId: number;
    mangaId: number;
    mangaChapterId: number;
    pages: number[];
}

export interface MangaWithChapters {
    manga: Manga;
    chapters: MangaChapter[];
    bookmarks: MangaBookmark[];
    favourite: boolean;
}

export interface PaginatedManga {
    pages: number;
    count: number;
    results: Manga[];
}

export interface MangaProgressUpdate {
    mangaId: number;
    mangaChapterId: number;
    page: number;
}

export interface MangaProgress {
    id: number;
    createdAt: Date;
    updatedAt: Date;
    deletedAt?: Date;

    profileId: number;
    mangaId: number;
    mangaChapterId: number;
    pageIndex: number;
}

export interface PaginatedMangaProgress {
    pages: number;
    count: number;
    results: MangaProgressData[];
}

export interface MangaProgressData {
    manga: Manga;
    progress?: MangaProgress;
    chapter: MangaChapter;
    stats: {
        maxChapterNum: number;
        chapterNum: number;
        pageCount: number;
        chapterProgress: number;
        pageProgress: number;
        favourite: boolean;
        bookmarks: number[];
    }
}

export interface MangaFilter {
    page: number;
    size: number;
    search?: string;
    asc: boolean;
    include: string[];
    exclude: string[];
    sort?: number;
}