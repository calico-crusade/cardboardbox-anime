import { Component, ElementRef, HostListener, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { ActivatedRoute, Router } from '@angular/router';
import { catchError, lastValueFrom, of } from 'rxjs';
import { PopupService, PopupComponent } from 'src/app/components';
import { AuthService, LightNovelService, MangaChapter, MangaService, MangaWithChapters, StorageVar } from 'src/app/services';

const DEFAULT_IMAGE = 'https://wallpaperaccess.com/full/1979093.jpg';

@Component({
    templateUrl: './manga-page.component.html',
    styleUrls: ['./manga-page.component.scss']
})
export class MangaPageComponent implements OnInit, OnDestroy {

    progressBarOptions = ['', 'bottom', 'left', 'right' ];
    sizeOptions = ['Fit to Height', 'Fit to Width', 'Natural Image Size'];

    @ViewChild('popup') popup!: PopupComponent;
    @ViewChild('scrollcont') el!: ElementRef<any>;
    @ViewChild('bookmarkspopup') bookmarkPop!: PopupComponent;

    loading: boolean = false;
    error?: string;

    id!: number;
    chapterId!: number;
    page!: number;

    data?: MangaWithChapters;

    chapter?: MangaChapter;
    bookmarks: number[] = [];
    get manga() { return this.data?.manga; }
    get chapters() { return this.data?.chapters || []; }

    settings = {
        invertControls: new StorageVar<boolean>(false, 'invert-controls'),
        imgSize: new StorageVar<string>('Fit to Height', 'img-size'),
        scroll: new StorageVar<boolean>(false, 'scroll-chapter'),
        hideHeader: new StorageVar<boolean>(false, 'hide-header', (v) => this.auth.showHeader = !v),
        invert: new StorageVar<boolean>(false, 'invert-image'),
        scrollAmount: new StorageVar<number>(100, 'scroll-amount'),
        progressBar: new StorageVar<string>('', 'progress-bar'),
        noDirectionalButton: new StorageVar<boolean>(false, 'no-directional-buttons'),
        hideExtraButtons: new StorageVar<boolean>(false, 'hide-extra-buttons'),
    };

    get loggedIn() { return !!this.auth.currentUser; }

    get pageImage() {
        if (!this.manga || !this.chapter) return DEFAULT_IMAGE;
        return this.chapter.pages[this.page - 1] || DEFAULT_IMAGE;
    }

    get nextPageImage() {
        if (!this.manga || !this.chapter) return DEFAULT_IMAGE;
        return this.chapter.pages[this.page] || DEFAULT_IMAGE;
    }

    get chapterIndex() {
        if (!this.manga || !this.chapter) return -1;

        return this.chapters.findIndex(a => a.id === this.chapter?.id);
    }

    get hasNextPage() {
        if (!this.manga || !this.chapter) return false;
        
        const p = this.page;
        if (p >= 0 && p < this.chapter.pages.length) return true;

        return this.hasNextChapter;
    }

    get hasNextChapter() {
        if (!this.manga || !this.chapter) return false;

        let c = this.chapterIndex;
        if (c == -1) return false;

        c += 1;
        if (c >= 0 && c < this.chapters.length) return true;
        
        return false;
    }

    get hasPreviousPage() {
        if (!this.manga || !this.chapter) return false;

        const p = this.page - 2;
        if (p >= 0 && p < this.chapter.pages.length) return true;

        return this.hasPreviousChapter;
    }

    get hasPreviousChapter() {
        if (!this.manga || !this.chapter) return false;

        let c = this.chapterIndex;
        if (c == -1) return false;

        c -= 1;
        if (c >= 0 && c < this.chapters.length) return true;
        
        return false;
    }

    get fitToWidth() { return this.settings.imgSize.value === 'Fit to Width'; }
    get fitToHeight() { return this.settings.imgSize.value === 'Fit to Height'; }

    constructor(
        private route: ActivatedRoute,
        private router: Router,
        private api: MangaService,
        private pop: PopupService,
        private lnApi: LightNovelService,
        private title: Title,
        private auth: AuthService
    ) { this.auth.showHeader = !this.settings.hideHeader.value; }

    proxy(url?: string) {
        if (!url) return '';
        return this.lnApi.corsFallback(url, 'manga-page');
    }

    @HostListener('window:keydown', ['$event'])
    keyDownEvent(event: KeyboardEvent) {
        if (!this.fitToWidth && 
            !this.settings.scroll.value) return;

        const pos = this.el.nativeElement.scrollTop;
        const offset = this.settings.scrollAmount.value;
        if (event.key == 'ArrowUp') { 
            this.el.nativeElement.scrollTop = (pos - offset);
            return;
        }
        if (event.key == 'ArrowDown') { 
            this.el.nativeElement.scrollTop = (pos + offset);
            return;
        }
    }

    @HostListener('window:keyup', ['$event'])
    keyEvent(event: KeyboardEvent) {
        if (event.key == 'ArrowLeft') { this.prevPage(); return; }

        if (event.key == 'ArrowRight') { this.nextPage(); return; }

        if (this.fitToWidth || 
            this.settings.scroll.value) return;

        if (event.key == 'ArrowUp') { this.prevPage(); return; }
        if (event.key == 'ArrowDown') { this.nextPage(); return; }
    }

    ngOnInit(): void {
        this.auth
            .onLogin
            .subscribe(t => this.process(true));

        this.route
            .params
            .subscribe(t => {
                this.id = +t['id'];
                this.chapterId = +t['chapter'];
                this.page = +t['page'];

                if (!this.id) return;

                this.process();
            });

    }

    ngOnDestroy(): void {
        this.title.setTitle(this.api.defaultTitle);
        this.auth.title = undefined;
        this.auth.showHeader = true;
    }

    private async process(force: boolean = false) {
        this.loading = true;
        
        try {
            this.data = await this.getMangaData(force);
        } catch (err) {
            this.loading = false;
            this.printState(err, 'Error loading manga', true);
            return;
        }

        if (!this.manga) {
            this.loading = false;
            return;
        }

        this.chapter = this.chapters.find(t => t.id === this.chapterId);
        if (!this.chapter) {
            this.loading = false;
            return;
        }

        if (this.chapter.pages.length === 0) {
            this.chapter.pages = await lastValueFrom(this.api.manga(this.id, this.chapterId));
            if (this.chapter.pages.length == 0) {
                this.loading = false;
                this.printState(null, 'Could not polyfill pages', true);
                return;
            }
        }

        this.bookmarks = this.data?.bookmarks.find(t => t.mangaChapterId === this.chapterId)?.pages || [];
        this.title.setTitle('CBA | ' + this.manga.title);
        this.auth.title = this.manga?.title;

        let p = this.page - 1;

        if (p < 0) p = 0;
        if (p >= this.chapter.pages.length) p = this.chapter.pages.length - 1;

        this.page = p + 1;
        this.progressUpdate();
        this.loading = false;
        this.printState(null, 'Manga State Updated');
    }

    progressUpdate() {
        if (!this.auth.currentUser) return;

        this.api
            .progress({
                mangaId: this.id,
                mangaChapterId: this.chapterId,
                page: this.page
            })
            .pipe(
                catchError(err => {
                    this.printState(err, 'Error occurred while updating progress', true);
                    return of({});
                })
            )
            .subscribe(t => {
                this.printState(null, 'Manga Progress Updated');
            });
    }

    async getMangaData(force: boolean) {
        if (this.manga && this.manga.id === this.id && !force) return this.data;

        if (!this.id) return undefined;

        return await lastValueFrom(this.api.manga(this.id));
    }

    navigate(page?: number, chapter?: number) {
        this.printState({ page, chapter });
        let p = page;
        if (page === undefined) p = this.page - 1;
        p = p || 0;
        this.router.navigate([ '/manga', this.id, chapter || this.chapterId, p + 1 ]);
    }

    pageChange(p: number) {
        this.printState(null, 'pageChange');

        if (!this.manga || !this.chapter) return;
        const c = this.chapterIndex;

        //New page index is within page bounds
        if (p >= 0 && p < this.chapter.pages.length) {
            this.navigate(p);
            return;
        }

        //Move to previous chapter
        if (p < 0 && c > 0) {
            const chapter = this.chapters[c - 1];
            this.navigate(999, chapter.id);
            return;
        }

        if (p >= this.chapter.pages.length && c < this.chapters.length) {
            const chapter = this.chapters[c + 1];
            this.navigate(0, chapter.id);
            return;
        }
        
        this.printState(null, 'No change detected', true);
    }

    printState(state?: any, mod?: string, error: boolean = false) {
        if (this.api.isProd && !error) return;

        let logger = error ? console.error : console.log;

        logger('State', {
            mod,
            loading: this.loading,
            error: this.error,
            id: this.id,
            chapterId: this.chapterId,
            chapter: this.chapter,
            page: this.page,
            manga: this.manga,
            state,
            image: this.pageImage,
            hasNextChapter: this.hasNextChapter,
            hasNextPage: this.hasNextPage,
            hasPreviousChapter: this.hasPreviousChapter,
            hasPreviousPage: this.hasPreviousPage,
            chapterIndex: this.chapterIndex,
            data: this.data
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

    bookmarkPage() {
        if (!this.chapter) return;

        const i = this.bookmarks.indexOf(this.page);
        if (i === -1) {
            this.bookmarks.push(this.page);
        } else {
            this.bookmarks.splice(i);
        }

        this.api
            .bookmark(this.chapter, this.bookmarks)
            .subscribe(t => {
                console.log('Page bookmarked');
                
                if (!this.data) return;

                if (!this.data.bookmarks)
                    this.data.bookmarks = [];

                const bk = this.data.bookmarks.findIndex(t => t.mangaChapterId === this.chapterId);
                if (bk === -1) {
                    this.data.bookmarks.push({
                        id: -1,
                        createdAt: new Date(),
                        updatedAt: new Date(),
                        profileId: -1,
                        mangaId: this.id,
                        mangaChapterId: this.chapterId,
                        pages: [this.page]
                    });
                    return;
                }

                this.data.bookmarks[bk].pages = this.bookmarks;
            });
    }

    showBookmarks() {
        this.pop.show(this.bookmarkPop);
    }

    getChapter(id: number) {
        return this.chapters.find(t => t.id === id);
    }

    imageClick(event: MouseEvent) {
        if (this.settings.scroll.value) return;

        const el = <HTMLElement>event.target;
        const rect = el.getBoundingClientRect();

        const isRight = this.settings.noDirectionalButton.value ||
            event.clientX >= (rect.x + rect.width) / 2;

        if (isRight && this.hasNextPage) {
            this.nextPage();
            return;
        }

        if (!isRight && this.hasPreviousPage) {
            this.prevPage();
            return;
        }
    }
}
