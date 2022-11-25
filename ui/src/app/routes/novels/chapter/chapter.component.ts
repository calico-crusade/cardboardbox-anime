import { Component, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { ActivatedRoute, Router } from '@angular/router';
import { lastValueFrom } from 'rxjs';
import { PopupComponent, PopupInstance, PopupService } from 'src/app/components';
import { DictionaryDefinitionService } from 'src/app/components/dictionary-definition/dictionary-definition.service';
import { AuthService, ChapterPages, LightNovelService, NovelBook, NovelChapter, Scaffold, SubscriptionHandler } from 'src/app/services';

@Component({
  templateUrl: './chapter.component.html',
  styleUrls: ['./chapter.component.scss']
})
export class ChapterComponent implements OnInit, OnDestroy {

    private _subs = new SubscriptionHandler();

    private _popIn?: PopupInstance;
    @ViewChild('popup') popup!: PopupComponent;

    loading: boolean = false;
    error?: string;

    scaffold?: Scaffold;

    seriesId?: number;
    bookId?: number;
    chapterId?: number;

    book?: NovelBook;
    bookNext?: NovelBook;
    bookPrev?: NovelBook;

    chapters: NovelChapter[] = [];
    chapter?: NovelChapter;
    chapterNext?: NovelChapter;
    chapterPrev?: NovelChapter;
    pages: ChapterPages[] = [];

    get series() { return this.scaffold?.series; }
    get source() { return this.series ? new URL(this.series.url || '').hostname.replace('www.', '') : ''; }
    

    constructor(
        private api: LightNovelService,
        private route: ActivatedRoute,
        private router: Router,
        private pop: PopupService,
        private auth: AuthService,
        private title: Title,
        private dic: DictionaryDefinitionService
    ) { }

    ngOnInit(): void {
        this._subs.subscribe(this.route.params, t => {
            this.seriesId = +t['id'];
            this.bookId = +t['bookId'];
            this.chapterId = +t['chapterId'];
            this.process();
        });
    }

    ngOnDestroy(): void {
        this.auth.title = undefined;
        this.title.setTitle(this.api.defaultTitle);
        this._subs.unsubscribe();
    }

    mouseEvent() {
        const data = document.getSelection();
        const text = data?.toString();
        if (!text) return;
        
        const words = text.split(' ');
        if (words.length > 4) return;

        this.dic.definition(text);
    }

    private async process() {
        if (!this.seriesId || !this.bookId || !this.chapterId) return;

        this.loading = true;

        try {
            const prom = this.api.chapter(this.bookId, this.chapterId).promise;
            this.scaffold = await lastValueFrom(this.api.seriesById(this.seriesId));
            this.pages = await prom;
        } catch (error: any) {
            console.error('Error occurred while fetching series', { error });
            this.error = error?.status;
            this.loading = false;
            return;
        }

        if (!this.scaffold) {
            this.error = `I couldn't find that book!`;
            this.loading = false;
            return;
        }

        const target = this.scaffold.books.find(t => t.book.id === this.bookId);
        if (!target) {
            this.error = `I couldn't find that book in the given series!`;
            this.loading = false;
            return;
        }
        this.book = target?.book;
        this.chapters = target?.chapters || [];
        this.chapter = this.chapters.find(t => t.id === this.chapterId);
        const ci = this.chapters.findIndex(t => t.id === this.chapterId);
        
        this.auth.title = this.book?.title;
        this.title.setTitle(this.book?.title);


        if (this.book) {
            this.bookNext = this.scaffold.books.find(t => t.book.ordinal === target.book.ordinal + 1)?.book;
            this.bookPrev = this.scaffold.books.find(t => t.book.ordinal === target.book.ordinal - 1)?.book;
        }

        if (this.chapter && ci !== -1) {
            this.chapterNext = this.chapters[ci + 1];
            this.chapterPrev = this.chapters[ci - 1];
        }

        this.loading = false;
    }

    move(item?: NovelChapter | NovelBook) {
        if (!item) return;

        if ('bookId' in item) {
            this.router.navigate(['/series', this.seriesId, 'book', this.bookId, 'chapter', item.id ]);
            return;
        }

        this.router.navigate(['/series', this.seriesId, 'book', item.id ]);
    }

    showChapters() {
        this._popIn = this.pop.show(this.popup);
        this._popIn.result.subscribe(t => {
            this._popIn = undefined;
        });
    }

    closeChapters() {
        this._popIn?.ok();
        this._popIn = undefined;
    }
}
