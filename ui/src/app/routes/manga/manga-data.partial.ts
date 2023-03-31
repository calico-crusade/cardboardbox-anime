import { MangaChapter, MangaWithChapters } from "../../services";
import { SettingsPartial } from "src/app/settings.partial";

const DEFAULT_IMAGE = '/assets/broken.webp';

export abstract class MangaPartial extends SettingsPartial {

    private _sort: ('chap' | 'release') = 'chap';

    data?: MangaWithChapters;
    
    get sort() { return this._sort; }
    set sort(value: ('chap' | 'release')) {
        this._sort = value;
        this.determineVolGroups();
    }

    get manga() { return this.data?.manga; }
    get chapters() { 
        const res = this.data?.chapters || [];
        
        if (this.sort === 'chap') return this.doSort(res, t => t.ordinal);
        return this.doSort(res, t => new Date(t.createdAt).getTime());
    }
    get favourite() { return this.data?.favourite ?? false; }
    get allBookmarks() { return this.data?.bookmarks || []; }
    get hasBookmarks() { return this.allBookmarks.length > 0; }

    doSort<TItem, TProp>(items: TItem[], prop: (a: TItem) => TProp) {
        return items.sort((a, b) => {
            const ap = prop(a),
                  bp = prop(b);
            if (ap < bp) return -1;
            if (ap > bp) return 1;
            return 0;
        });
    }

    public abstract determineVolGroups(): void;
}

export abstract class MangaPagePartial extends MangaPartial {

    chapterId!: number;
    page!: number;

    chapter?: MangaChapter;
    bookmarks: number[] = [];

    pageImage: string = DEFAULT_IMAGE;
    nextPageImage: string = DEFAULT_IMAGE;
    chapterIndex: number = 0;
    hasNextPage: boolean = false;
    hasNextChapter: boolean = false;
    hasPreviousPage: boolean = false;
    hasPreviousChapter: boolean = false;

    updateProperties() {
        this.chapter = this.chapters.find(t => t.id === this.chapterId);
        this.bookmarks = this.allBookmarks.find(t => t.mangaChapterId === this.chapterId)?.pages || [];

        this.pageImage = (() => {
            if (!this.manga || !this.chapter) return DEFAULT_IMAGE;
            return this.chapter.pages[this.page - 1] || DEFAULT_IMAGE;
        })();

        this.nextPageImage = (() => {
            if (!this.manga || !this.chapter) return DEFAULT_IMAGE;
            return this.chapter.pages[this.page] || DEFAULT_IMAGE;
        })();

        this.chapterIndex = (() => {
            if (!this.manga || !this.chapter) return -1;
            return this.chapters.findIndex(a => a.id === this.chapter?.id);
        })();

        this.hasNextChapter = (() => {
            if (!this.manga || !this.chapter) return false;
    
            let c = this.chapterIndex;
            if (c == -1) return false;
    
            c += 1;
            if (c >= 0 && c < this.chapters.length) return true;
    
            return false;
        })();

        this.hasNextPage = (() => {
            if (!this.manga || !this.chapter) return false;
            const p = this.page;
            if (p >= 0 && p < this.chapter.pages.length) return true;
            return this.hasNextChapter;
        })();

        this.hasPreviousChapter = (() => {
            if (!this.manga || !this.chapter) return false;
    
            let c = this.chapterIndex;
            if (c == -1) return false;
    
            c -= 1;
            if (c >= 0 && c < this.chapters.length) return true;
    
            return false;
        })();

        this.hasPreviousPage = (() => {
            if (!this.manga || !this.chapter) return false;
    
            const p = this.page - 2;
            if (p >= 0 && p < this.chapter.pages.length) return true;
    
            return this.hasPreviousChapter;
        })();
    }

}