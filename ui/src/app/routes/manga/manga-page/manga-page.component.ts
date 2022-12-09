import { Component, ElementRef, HostListener, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { ActivatedRoute, Router } from '@angular/router';
import { catchError, of } from 'rxjs';
import { PopupService, PopupComponent } from 'src/app/components';
import { AuthService, LightNovelService, MangaService, StorageVar, SubscriptionHandler, UtilitiesService } from 'src/app/services';
import { MangaPagePartial } from '../manga-data.partial';

@Component({
    templateUrl: './manga-page.component.html',
    styleUrls: ['./manga-page.component.scss']
})
export class MangaPageComponent extends MangaPagePartial implements OnInit, OnDestroy {

    private _subs = new SubscriptionHandler();

    progressBarOptions = ['', 'bottom', 'left', 'right'];
    sizeOptions = ['Fit to Height', 'Fit to Width', 'Natural Image Size'];
    filters = ['', 'invert', 'blue-light', 'blue-print', 'custom'];

    @ViewChild('popup') popup!: PopupComponent;
    @ViewChild('scrollcont') el!: ElementRef<any>;
    @ViewChild('bookmarkspopup') bookmarkPop!: PopupComponent;
    @ViewChild('links') linksPop!: PopupComponent;

    loading: boolean = false;
    error?: string;
    downloading: boolean = false;
    isExternal: boolean = false;

    id!: string;
    
    constructor(
        private route: ActivatedRoute,
        private router: Router,
        private api: MangaService,
        private pop: PopupService,
        private lnApi: LightNovelService,
        private title: Title,
        private auth: AuthService,
        private util: UtilitiesService
    ) { super(auth); }

    proxy(url?: string, referer?: string) {
        if (!url) return '';
        return this.lnApi.corsFallback(url, 'manga-page', referer);
    }

    @HostListener('window:keydown', ['$event'])
    keyDownEvent(event: KeyboardEvent) {
        if (!this.fitToWidth &&
            !this.mangaScroll) return;

        const pos = this.el.nativeElement.scrollTop;
        const offset = this.mangaScrollAmount;
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

        if (this.fitToWidth || this.mangaScroll) return;

        if (event.key == 'ArrowUp') { this.prevPage(); return; }
        if (event.key == 'ArrowDown') { this.nextPage(); return; }
    }

    ngOnInit(): void {
        this.initManga();
        this._subs
            .subscribe(this.auth.onLogin, t => this.process(true))
            .subscribe(this.route.params, t => {
                this.id = t['id'];
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
        this._subs.unsubscribe();
    }

    private async process(force: boolean = false) {
        this.loading = true;

        try {
            this.data = await this.getMangaData(force);
            this.updateProperties();
        } catch (err) {
            this.loading = false;
            this.printState(err, 'Error loading manga', true);
            return;
        }

        if (!this.manga) {
            this.loading = false;
            return;
        }

        if (!this.chapter) {
            this.loading = false;
            return;
        }

        if (this.chapter.pages.length === 0) {
            this.chapter.pages = await this.api.manga(this.manga.id, this.chapterId).promise;
            if (this.chapter.pages.length == 0) {
                if (this.chapter.externalUrl) {
                    this.isExternal = true;
                    this.loading = false;
                    return;
                }

                this.loading = false;
                this.printState(null, 'Could not polyfill pages', true);
                return;
            }
        }

        this.title.setTitle('CBA | ' + this.manga.title);
        this.auth.title = this.manga?.title;

        let p = this.page - 1;

        if (p < 0) p = 0;
        if (p >= this.chapter.pages.length) p = this.chapter.pages.length - 1;
        this.page = p + 1;
        this.updateProperties();
        this.progressUpdate();
        this.loading = false;
        this.printState(null, 'Manga State Updated');
    }

    progressUpdate() {
        if (!this.auth.currentUser || !this.manga) return;

        this.api
            .progress({
                mangaId: this.manga.id,
                mangaChapterId: this.chapterId,
                page: this.page
            })
            .error(err => {
                this.printState(err, 'Error occurred while updating progress', true);
            }, {})
            .subscribe(t => {
                this.printState(null, 'Manga Progress Updated');
            });
    }

    async getMangaData(force: boolean) {
        if (this.manga && (this.manga.id.toString() === this.id || this.manga.hashId == this.id) && !force) return this.data;

        if (!this.id) return undefined;

        return await this.api.manga(this.id).promise;
    }

    navigate(page?: number, chapter?: number) {
        this.printState({ page, chapter });
        let p = page;
        if (page === undefined) p = this.page - 1;
        p = p || 0;
        this.router.navigate(['/manga', this.id, chapter || this.chapterId, p + 1]);
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
        if (this.mangaInvertControls)
            this.pageChange(this.page - 2);
        else
            this.pageChange(this.page);
    }

    prevPage() {
        if (this.mangaInvertControls)
            this.pageChange(this.page);
        else
            this.pageChange(this.page - 2);
    }

    nextChap() {
        if (this.mangaInvertControls)
            this.pageChange(-1)
        else
            this.pageChange(999);
    }

    prevChap() {
        if (this.mangaInvertControls)
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
                if (!this.data || !this.manga) return;

                if (!this.data.bookmarks)
                    this.data.bookmarks = [];

                const bk = this.data.bookmarks.findIndex(t => t.mangaChapterId === this.chapterId);
                if (bk === -1) {
                    this.data.bookmarks.push({
                        id: -1,
                        createdAt: new Date(),
                        updatedAt: new Date(),
                        profileId: -1,
                        mangaId: this.manga?.id,
                        mangaChapterId: this.chapterId,
                        pages: [this.page]
                    });
                    return;
                }

                this.data.bookmarks[bk].pages = this.bookmarks;
            });
    }

    showBookmarks() { this.pop.show(this.bookmarkPop); }

    imageClick(event: MouseEvent) {
        if (this.mangaScroll) return;

        const el = <HTMLElement>event.target;
        const rect = el.getBoundingClientRect();

        const isRight = this.mangaNoDirectionalButton ||
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

    async share() { await navigator.clipboard.writeText(window.location.href); }

    openLinks() { this.pop.show(this.linksPop); }

    resetOptions() {
        for(let settings of this.mangaSettings) settings.value = undefined;
    }

    fullscreen() {
        const elem = <any>document.documentElement;
        const doc = <any>document;

        const isFullScreen = () => {
            return ((<any>window).fullScreen) || 
                (window.innerWidth == screen.width && window.innerHeight == screen.height) ||
                (!window.screenTop && !window.screenY);
        };

        if (isFullScreen()) {
            if (document.exitFullscreen) {
                document.exitFullscreen();
            } else if (doc.webkitExitFullscreen) { /* Safari */
                doc.webkitExitFullscreen();
            } else if (doc.msExitFullscreen) { /* IE11 */
                doc.msExitFullscreen();
            }
            return;
        }

        if (elem.requestFullscreen) {
            elem.requestFullscreen();
        } else if (elem.webkitRequestFullscreen) { /* Safari */
            elem.webkitRequestFullscreen();
        } else if (elem.msRequestFullscreen) { /* IE11 */
            elem.msRequestFullscreen();
        }
    }

    download(url: string) {
        this.downloading = true;
        this.util
            .download(url)
            .pipe(
                catchError(error => {
                    console.error('Error occurred while downloading file: ', {
                        url,
                        error
                    });
                    this.error = 'An error occurred while attempting to download your file!';
                    setTimeout(() => {
                        this.error = undefined;
                    }, 3000);
                    return of(undefined);
                })
            ).subscribe(t => {
                this.downloading = false;
            });
    }

    imageFilter() {
        let filters: { [key: string]: string } = {
            'brightness': this.mangaBrightness + '%'
        };

        switch(this.mangaFilter) {
            case 'invert': filters['invert'] = '100%'; break;
            case 'blue-light': 
                filters['sepia'] = '40%';
                filters['saturate'] = '200%';
                break;
            case 'blue-print':
                filters['sepia'] = '100%';
                filters['saturate'] = '500%';
                filters['hue-rotate'] = '180deg';
                break;
            case 'custom': return this.mangaCustomFilter;
        }

        return Object.keys(filters)
            .map(key => `${key}(${filters[key]})`)
            .join(' ');
    }
}
