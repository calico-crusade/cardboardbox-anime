import { Component, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { PopupComponent, PopupService } from 'src/app/components';
import { LightNovelService, Manga, MangaChapter, MangaService, MangaWithChapters, SubscriptionHandler } from 'src/app/services';

type Selected = {
    page: number;
    chapterId: number;
    url: string;
}

@Component({
    templateUrl: './manga-strip-maker.component.html',
    styleUrls: ['./manga-strip-maker.component.scss']
})
export class MangaStripMakerComponent implements OnInit, OnDestroy {

    private _subs = new SubscriptionHandler();

    @ViewChild('viewpop') viewPop!: PopupComponent;

    loading: boolean = false;
    error?: string;
    id!: string;
    chapterId!: number;
    page!: number;
    selected: Selected[] = [];
    viewUrl?: string;

    data?: MangaWithChapters;
    manga?: Manga;
    chapter?: MangaChapter;
    chapters: MangaChapter[] = [];

    constructor(
        private route: ActivatedRoute,
        private api: MangaService,
        private ln: LightNovelService,
        private pop: PopupService
    ) { }

    ngOnInit(): void {
        this._subs
            .subscribe(this.route.params, t => {
                this.id = t['id'];
                this.chapterId = +t['chapter'];
                this.page = +t['page'];
                this.process();
            });
    }

    ngOnDestroy(): void { this._subs.unsubscribe(); }

    proxy(url?: string) { return url ? this.ln.corsFallback(url, 'manga-page') : ''; }

    del(index: number) { this.selected.splice(index, 1); }

    view(url: string) {
        this.viewUrl = url;
        this.pop.show(this.viewPop);
    }

    async selectChapter() {
        this.chapterId = +this.chapterId;
        const chap = this.chapters.find(t => t.id === this.chapterId);
        if (!chap || !this.manga) return;

        this.chapter = chap;
        if (this.chapter.pages.length === 0) {
            this.chapter.pages = await this.api.manga(this.manga.id, this.chapterId).promise;
            if (this.chapter.pages.length === 0) {
                this.error = `Couldn't find pages for the current chapter`;
                return;
            }
        }
    }

    select(index: number, url: string) {
        this.selected.push({
            chapterId: this.chapterId,
            page: index + 1,
            url
        });
    }

    download() {
        if (!this.manga) return;

        this.loading = true;
        this.api
            .strip({
                mangaId: this.manga?.id,
                pages: this.selected.map(t => {
                    return {
                        chapterId: t.chapterId,
                        page: t.page
                    }
                })
            })
            .subscribe(t => {
                this.loading = false;
            });
    }

    isActive(url: string) {
        return !!this.selected.find(t => t.url === url);
    }

    move(index: number, inc: number) {
        let i = index + inc;
        if (i < 0) i = this.selected.length - 1;
        else if (i + 1 >= this.selected.length) i = 0;
        const current = this.selected[index];
        const target = this.selected[i];
        this.selected[index] = target;
        this.selected[i] = current;
    }

    private async process() {
        this.loading = true;
        this.error = undefined;
        await this.getData();
        this.loading = false;
    }

    private async getData() {
        try {
            this.data = await this.api.manga(this.id).promise;
        } catch (error: any) {
            this.error = error?.toString();
            console.error('Error fetching manga', { 
                error,
                id: this.id,
                chapter: this.chapterId,
                page: this.page
            });
        }

        this.manga = this.data?.manga;
        this.chapters = this.data?.chapters || [];
        this.chapter = this.chapters.find(t => t.id === this.chapterId);

        if (!this.manga || !this.chapter) return;

        if (this.chapter.pages.length === 0) {
            this.chapter.pages = await this.api.manga(this.manga.id, this.chapterId).promise;
            if (this.chapter.pages.length === 0) {
                this.error = `Couldn't find pages for the current chapter`;
                return;
            }
        }

        const curPage = this.chapter.pages[this.page];
        if (curPage === undefined) return;

        this.selected = [
            { page: this.page, chapterId: this.chapterId, url: curPage },
        ];
    }
}
