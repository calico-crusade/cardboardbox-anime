import { Component, HostListener, OnInit, ViewChild } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { lastValueFrom } from 'rxjs';
import { PopupService, PopupComponent } from 'src/app/components';
import { Manga, MangaChapter, MangaService } from 'src/app/services';

const DEFAULT_IMAGE = 'https://wallpaperaccess.com/full/1979093.jpg';

class StorageVar<T> {
    constructor(
        public defValue: T,
        public name: string
    ) { }

    get value(): T {
        return <any>localStorage.getItem(this.name) || this.defValue;
    }

    set value(item: T | undefined) {
        if (!item) {
            localStorage.removeItem(this.name);
            return;
        }

        localStorage.setItem(this.name, <any>item);
    }
}

@Component({
    templateUrl: './manga-page.component.html',
    styleUrls: ['./manga-page.component.scss']
})
export class MangaPageComponent implements OnInit {

    @ViewChild('popup') popup!: PopupComponent;

    loading: boolean = false;
    error?: string;

    url!: string;
    chapter!: string;
    page!: number;

    manga?: Manga;
    mangaChapter?: MangaChapter;

    settings = {
        invertControls: new StorageVar<boolean>(false, 'invert-controls'),
        fitToWidth: new StorageVar<boolean>(false, 'fit-to-width'),
        scroll: new StorageVar<boolean>(false, 'scroll-chapter'),
        hideHeader: new StorageVar<boolean>(false, 'hide-header')
    };

    get pageImage() {
        if (!this.manga || !this.mangaChapter) return DEFAULT_IMAGE;
        return this.mangaChapter.pages[this.page - 1] || DEFAULT_IMAGE;
    }

    get nextPageImage() {
        if (!this.manga || !this.mangaChapter) return DEFAULT_IMAGE;
        return this.mangaChapter.pages[this.page] || DEFAULT_IMAGE;
    }

    get chapterIndex() {
        if (!this.manga || !this.mangaChapter) return -1;

        return this.chapters.findIndex(a => a.id === this.mangaChapter?.id);
    }

    get chapters() {
        return this.manga?.chapters.sort((a, b) => {
            if (a.number < b.number) return -1;
            if (a.number > b.number) return 1;
            return 0;
        }) || [];
    }

    get hasNextPage() {
        if (!this.manga || !this.mangaChapter) return false;
        
        const p = this.page;
        if (p >= 0 && p < this.mangaChapter.pages.length) return true;

        return this.hasNextChapter;
    }

    get hasNextChapter() {
        if (!this.manga || !this.mangaChapter) return false;

        let c = this.chapterIndex;
        if (c == -1) return false;

        c += 1;
        if (c >= 0 && c < this.manga.chapters.length) return true;
        
        return false;
    }

    get hasPreviousPage() {
        if (!this.manga || !this.mangaChapter) return false;

        const p = this.page - 2;
        if (p >= 0 && p < this.mangaChapter.pages.length) return true;

        return this.hasPreviousChapter;
    }

    get hasPreviousChapter() {
        if (!this.manga || !this.mangaChapter) return false;

        let c = this.chapterIndex;
        if (c == -1) return false;

        c -= 1;
        if (c >= 0 && c < this.manga.chapters.length) return true;
        
        return false;
    }

    constructor(
        private route: ActivatedRoute,
        private router: Router,
        private api: MangaService,
        private pop: PopupService
    ) { }

    @HostListener('window:keyup', ['$event'])
    keyEvent(event: KeyboardEvent) {
        if (event.key == 'ArrowLeft' ||
            event.key == 'ArrowUp') { this.prevPage(); return; }

        if (event.key == 'ArrowRight' ||
            event.key == 'ArrowDown') { this.nextPage(); return; }
    }

    ngOnInit(): void {
        this.route
            .params
            .subscribe(t => {
                this.url = t['url'];
                this.chapter = t['chapter'];
                this.page = +t['page'];
                this.process();
            });
    }

    private async process() {
        this.loading = true;
        await Promise.all([ this.getManga(), this.getChapter() ]);

        if (!this.manga || !this.mangaChapter) {
            this.loading = false;
            return;
        }

        let p = this.page - 1;

        if (p < 0) p = 0;
        if (p >= this.mangaChapter.pages.length) p = this.mangaChapter.pages.length - 1;

        this.page = p + 1;

        this.loading = false;
    }

    private async getManga() {
        try {
            if (this.manga && this.manga.homePage == this.url) return;
            this.manga = await lastValueFrom(this.api.manga(this.url));
            this.printState(null, 'Manga Fetch');
        } catch (err) {
            this.printState(err, 'Manga Fetch Error');
        }
    }

    private async getChapter() {
        try {
            if (this.mangaChapter && this.mangaChapter.id == this.chapter) return;

            this.mangaChapter = await lastValueFrom(this.api.chapter(this.url, this.chapter));

            this.printState(null, 'Chapter Fetch');
        } catch (err) {
            this.printState(err, 'Chapter Fetch Error');
        }
    }

    navigate(page?: number, chapter?: string) {
        this.printState({ page, chapter });
        let p = page;
        if (page === undefined) p = this.page - 1;
        p = p || 0;
        this.router.navigate([ '/manga', this.url, chapter || this.chapter, p + 1 ]);
    }

    pageChange(p: number) {
        this.printState(null, 'pageChange');

        if (!this.manga || !this.mangaChapter) return;
        const c = this.chapterIndex;

        //New page index is within page bounds
        if (p >= 0 && p < this.mangaChapter.pages.length) {
            this.navigate(p);
            return;
        }

        //Move to previous chapter
        if (p < 0 && c > 0) {
            const chapter = this.manga.chapters[c - 1];
            this.navigate(999, chapter.id);
            return;
        }

        if (p >= this.mangaChapter.pages.length && c < this.manga.chapters.length) {
            const chapter = this.manga.chapters[c + 1];
            this.navigate(0, chapter.id);
            return;
        }
        
        this.printState(null, 'No change detected');
    }

    printState(state?: any, mod?: string) {
        console.log('State', {
            mod,
            loading: this.loading,
            error: this.error,
            url: this.url,
            chapter: this.chapter,
            page: this.page,
            manga: this.manga,
            mangaChapter: this.mangaChapter,
            state,
            image: this.pageImage,
            hasNextChapter: this.hasNextChapter,
            hasNextPage: this.hasNextPage,
            hasPreviousChapter: this.hasPreviousChapter,
            hasPreviousPage: this.hasPreviousPage,
            chapterIndex: this.chapterIndex
        });
    }

    settingsShow() {
        this.pop.show(this.popup);
    }

    nextPage() {
        if (this.settings.invertControls.value)
            this.pageChange(this.page - 2);
        else
            this.pageChange(this.page);
    }

    prevPage() {
        if (this.settings.invertControls.value)
            this.pageChange(this.page);
        else
            this.pageChange(this.page - 2);
    }

    nextChap() {
        if (this.settings.invertControls.value)
            this.pageChange(-1)
        else
            this.pageChange(999);
    }

    prevChap() {
        if (this.settings.invertControls.value)
            this.pageChange(999);
        else
            this.pageChange(-1);

    }
}
