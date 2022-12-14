import { Component, OnDestroy, OnInit } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { ActivatedRoute } from '@angular/router';
import { catchError, of } from 'rxjs';
import { AuthService, LightNovelService, NovelBook, NovelChapter, Scaffold, SubscriptionHandler, UtilitiesService } from './../../../services';

@Component({
    templateUrl: './book.component.html',
    styleUrls: ['./book.component.scss']
})
export class BookComponent implements OnInit, OnDestroy {

    private _subs = new SubscriptionHandler();

    loading: boolean = false;
    downloading: boolean = false;
    error?: string;
    
    seriesId!: number;
    bookId!: number;

    scaffold?: Scaffold;
    book?: NovelBook;
    chapters: NovelChapter[] = [];

    nextBook?: NovelBook;
    prevBook?: NovelBook;
    
    get series() { return this.scaffold?.series; }

    get source() { 
        if (!this.series) return '';
        return new URL(this.series.url || '').hostname.replace('www.', '');
    }

    get attribution() {
        if (!this.series || !this.book) return [];

        const uiq = (array: string[]) => array.filter((v, i, s) => s.indexOf(v) === i);

        return [
            {
                type: 'Author',
                value: uiq([...this.series.authors, ...this.book.authors])
            }, {
                type: 'Illustrator',
                value: uiq([...this.series.illustrators, ...this.book.illustrators])
            }, {
                type: 'Editor',
                value: uiq([...this.series.editors, ...this.book.editors])
            }, {
                type: 'Translator',
                value: uiq([...this.series.translators, ...this.book.translators])
            }
        ]
    }

    constructor(
        private route: ActivatedRoute,
        private api: LightNovelService,
        private auth: AuthService,
        private title: Title,
        private util: UtilitiesService
    ) { }

    ngOnInit(): void {
        this._subs
            .subscribe(this.route.params, t => {
                this.seriesId = +t['id'];
                this.bookId = +t['bookId'];
                this.process();
            });
    }

    ngOnDestroy(): void {
        this.title.setTitle(this.api.defaultTitle);
        this.auth.title = undefined;
        this._subs.unsubscribe();
    }

    private process() {
        this.loading = true;
        this.api
            .seriesById(this.seriesId)
            .pipe(
                catchError(err => {
                    this.error = `I couldn't find that book!`;
                    console.error('Error occurred while requesting series', {
                        this: this,
                        err
                    })
                    return of();
                })
            )
            .subscribe(t => {
                this.scaffold = t;
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
                setTimeout(() => {
                    this.title.setTitle(this.book?.title || 'CardboardBox | Anime');
                    this.auth.title = this.book?.title;
                }, 100);

                if (this.book) {
                    this.nextBook = this.scaffold.books.find(t => t.book.ordinal === target.book.ordinal + 1)?.book;
                    this.prevBook = this.scaffold.books.find(t => t.book.ordinal === target.book.ordinal - 1)?.book;
                }

                this.loading = false;
            });
    }

    download() {
        if (!this.book) return;

        this.downloading = true;
        const url = this.api.downloadUrl(this.book);

        this.util
            .download(url)
            .pipe(
                catchError(error => {
                    console.error('Error occurred while downloading file: ', {
                        error,
                        url,
                        scaffold: this.scaffold
                    });
                    this.error = 'An error occurred while attempting to download your file(s)!';
                    setTimeout(() => {
                        this.error = undefined;
                    }, 3000);
                    return of(undefined);
                })
            )
            .subscribe(t => {
                this.downloading = false;
            });
    }
}
