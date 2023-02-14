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

    attributes: {
        name: string;
        value: string;
    }[];
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

export interface MangaProgressStats {
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

export interface MangaProgressData {
    manga: Manga;
    progress?: MangaProgress;
    chapter: MangaChapter;
    stats: MangaProgressStats;
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

export interface ImageSearchManga {
    title: string;
    id: string;
    url: string;
    description: string;
    source: string;
    nsfw: boolean;
    cover: string;
    tags: string[];
}

export interface BaseResult {
    score: number;
    exactMatch: boolean;
    manga: ImageSearchManga;
}

export interface VisionResult extends BaseResult {
    url: string;
    title: string;
    filteredTitle: string;
}

export interface MatchResult extends BaseResult {
    metadata: {
        id: string;
        url: string;
        source: string;
        type: number;
        mangaId: string;
        chapterId?: string;
        page?: number;
    };
}

export interface ImageSearch {
    vision: VisionResult[];
    match: MatchResult[];
    textual: BaseResult[];

    bestGuess?: ImageSearchManga;
}

export interface MangaGraph {
    type: string;
    key: string;
    count: number;
}