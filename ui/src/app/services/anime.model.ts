export interface VrvAnime {
    id: string;
    link: string;
    title: string;
    description: string;
    type: string;
    channelId: string;
    
    images: {
        width: number;
        height: number;
        type: string;
        source: string;
    }[];

    metadata: {
        mature: boolean;
        matureBlocked: boolean;
        subbed: boolean;
        dubbed: boolean;
        ratings: string[];
        series?: {
            episodeCount: number;
            seasonCount: number;
            simulcast: boolean;
            lastPublicSeasonNumber: number;
            lastPublicEpisodeNumber: number;
            tenantCategories: string[];
        };
        movies?: {
            firstMovieId: string;
            durationMs: number;
            movieReleaseYear: number;
            premiumOnly: boolean;
            availableOffline: boolean;
        }
    }
}

export interface Image {
    width?: number;
    height?: number;
    type: string;
    source: string;
    platformId: string;
}

export interface Anime {
    id: string;
    hashId: string;
    animeId: string;
    link: string;
    title: string;
    description: string;
    type: string;
    platformId: string;
    
    images: Image[];

    metadata: {
        languages: string[];
        languageTypes: string[];
        ratings: string[];
        tags: string[];
        mature: boolean;
        seasons: {
            episodeCount: number;
            type: string;
            order: number;
            number: number;
            altTitle?: string;
            id: string;
        }[],
        ext: { [key: string]: string; }
    }
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
}