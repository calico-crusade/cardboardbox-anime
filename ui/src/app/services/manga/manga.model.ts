export interface Manga {
    title: string;
    id: string;
    provider: string;
    homePage: string;
    cover: string;
    tags: string[];
    chapters: {
        title: string;
        url: string;
        id: string;
        number: number;
    }[];
}

export interface MangaChapter {
    title: string;
    url: string;
    id: string;
    number: number;
    pages: string[];
}