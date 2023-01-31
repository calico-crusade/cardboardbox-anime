import { Component, OnInit, Input } from '@angular/core';
import { 
    BaseResult, LightNovelService, 
    Manga, MangaProgressData, MangaProgress, MangaProgressStats, MangaChapter, 
    MatchResult, VisionResult, ImageSearchManga 
} from 'src/app/services';

export type SearchData = {
    manga: ImageSearchManga;
    foundVia?: {
        text: string;
        compute: number;
        exactMatch: boolean;
    };
    link?: {
        text: string;
        url: string;
    }
};

export type CardData = {
    manga: Manga;
    progress?: MangaProgress;
    stats?: MangaProgressStats;
    chapter?: MangaChapter;
    icon?: {
        text: string;
        fill?: boolean;
    };
}

@Component({
  selector: 'cba-manga-card',
  templateUrl: './manga-card.component.html',
  styleUrls: ['./manga-card.component.scss']
})
export class MangaCardComponent implements OnInit {

    private _data?: Manga | MangaProgressData;
    private _search?: MatchResult | VisionResult | BaseResult | ImageSearchManga;

    @Input() 
    set search(value: MatchResult | VisionResult | BaseResult | ImageSearchManga | undefined) {
        this._search = value;
        this.searchInfo = this.getSearch();
        this.cardInfo = undefined;
    }

    @Input() 
    set data(value: Manga | MangaProgressData | undefined) {
        this._data = value;
        this.searchInfo = undefined;
        this.cardInfo = this.getCard();
    }

    cardInfo?: CardData;
    searchInfo?: SearchData | undefined;

    constructor(
        private lnApi: LightNovelService
    ) { }

    ngOnInit(): void {
    }

    proxy(url?: string) {
        if (!url) return '';
        return this.lnApi.corsFallback(url, 'manga-covers');
    }

    private domain(url: string) { return new URL(url).hostname; }

    private getCard(): CardData | undefined {
        if (!this._data) return undefined;
        const manga = ('id' in this._data) ? this._data : this._data.manga;

        if (!this._data || 'id' in this._data) return { manga };

        const { stats, progress, chapter } = this._data;

        const getIcon = () => {
            if (stats?.favourite) return { text: 'star', fill: true };
            if (stats?.chapterProgress === 100) return { text: 'check_circle' };
            if (stats?.hasBookmarks) return { text: 'bookmarks' };
            if (progress) return { text: 'collections_bookmark' };
            return undefined;
        }

        return {
            manga,
            progress: progress,
            stats: stats,
            chapter: chapter,
            icon: getIcon()
        }
    }

    private getSearch(): SearchData | undefined {
        if (!this._search) return undefined;

        if ('description' in this._search) {
            return {
                manga: this._search
            }
        }

        if ('filteredTitle' in this._search) {
            return {
                manga: this._search.manga,
                foundVia: {
                    text: 'Google Vision',
                    compute: this._search.score * 100,
                    exactMatch: this._search.exactMatch
                },
                link: {
                    text: this.domain(this._search.url),
                    url: this._search.url
                }
            }
        }

        if ('metadata' in this._search) {
            return {
                manga: this._search.manga,
                foundVia: {
                    text: 'CBA Reverse Search',
                    compute: this._search.score,
                    exactMatch: this._search.exactMatch
                },
                link: {
                    text: `${this._search.manga.source}: Page #${this._search.metadata.page}`,
                    url: `https://mangadex.org/chapter/${this._search.metadata.chapterId}/${this._search.metadata.page}`
                }
            }
        }

        return {
            manga: this._search.manga,
            foundVia: {
                text: 'Mangadex Search',
                compute: this._search.score * 100,
                exactMatch: this._search.exactMatch
            },
            link: {
                text: `Manga Home Page`,
                url: `https://mangadex.org/manga/${this._search.manga.id}`
            }
        }
    }
}
