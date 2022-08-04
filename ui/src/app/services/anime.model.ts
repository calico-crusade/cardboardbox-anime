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