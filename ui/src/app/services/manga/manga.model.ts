export interface Manga {
    id: number;
    createdAt: Date;
    updatedAt: Date;
    deletedAt?: Date;

    hashId: string;
    title: string;
    sourceId: string;
    provider: string;
    url: string;
    cover: string;
    tags: string[];
    altTitles: string[];
    description: string;
    nsfw: boolean;
    referer?: string;

    attributes: {
        name: string;
        value: string;
    }[];
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
    externalUrl?: string;
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
        hasBookmarks: boolean;
        latestChapter?: Date;
        completed: boolean;
        firstChapterId: number;
        progressChapterId?: number;
        progressId?: number;
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
    state?: number;
    nsfw?: number;
}

export interface MangaStrip {
    chapterId: number;
    page: number;
}

export interface MangaStripReq {
    mangaId: number;
    pages: MangaStrip[];
}