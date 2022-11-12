import { Component, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { ActivatedRoute } from '@angular/router';
import { catchError, of } from 'rxjs';
import { PopupComponent, PopupService } from 'src/app/components';
import { AuthService, LightNovelService, MangaProgress, MangaService, MangaWithChapters } from 'src/app/services';

@Component({
    templateUrl: './manga.component.html',
    styleUrls: ['./manga.component.scss']
})
export class MangaComponent implements OnInit, OnDestroy {

    @ViewChild('bookmarkspopup') bookmarkPop!: PopupComponent;

    loading: boolean = false;
    error?: string;
    id!: number;
    data?: MangaWithChapters;
    progress?: MangaProgress;

    get manga() {
        return this.data?.manga;
    }

    get chapters() {
        return this.data?.chapters || [];
    }

    get currentChapter() {
        return this.chapters.find(t => t.id === this.progress?.mangaChapterId);
    }

    get favourite() {
        return this.data?.favourite ?? false;
    }

    get loggedIn() {
        return !!this.auth.currentUser;
    }

    get hasBookmarks() {
        return (this.data?.bookmarks.length || 0) > 0;
    }

    constructor(
        private route: ActivatedRoute,
        private api: MangaService,
        private lnApi: LightNovelService,
        private title: Title,
        private auth: AuthService,
        private pop: PopupService
    ) { }

    ngOnInit(): void {
        this.route.params.subscribe(t => {
            this.id = +t['id'];
            this.process();
        });
        this.auth.onLogin.subscribe(t => {
            if (t) this.getProgress();
        });
    }

    ngOnDestroy(): void {
        this.title.setTitle(this.api.defaultTitle);
        this.auth.title = undefined;
    }

    private process() {
        this.loading = true;
        this.api
            .manga(this.id)
            .pipe(
                catchError(err => {
                    return of(undefined);
                })
            )
            .subscribe(t => {
                this.data = t;
                this.title.setTitle('CBA | ' + this.manga?.title);
                this.auth.title = this.manga?.title;
                this.loading = false;
            });

        if (this.auth.currentUser)
            this.getProgress();
    }

    private getProgress() {
        if (this.progress?.mangaId === this.id) return;

        this.api
            .progress(this.id)
            .pipe(
                catchError(err => {
                    console.error('Error occurred while getting progress', { err });
                    return of(undefined);
                })
            )
            .subscribe(t => {
                this.progress = t;
            });
    }

    update() {
        if (!this.manga) return;

        this.loading = true;
        this.api
            .reload(this.manga)
            .pipe(
                catchError(err => {
                    this.error = 'An error occurred while refreshing the manga!';
                    console.error('Error occurred!', {
                        manga: this.manga,
                        chapters: this.chapters,
                        id: this.id,
                        err
                    })
                    return of(undefined);
                })
            )
            .subscribe(t => {
                this.data = t;
                this.loading = false;
            })
    }

    toggleFavourite() {
        if (!this.loggedIn) return;
        this.api
            .favourite(this.id)
            .subscribe(t => {
                if (!this.data) return;
                this.data.favourite = t;
            });
    }

    proxy(url?: string) {
        if (!url) return '';
        return this.lnApi.corsFallback(url, 'manga-covers');
    }

    showBookmarks() {
        this.pop.show(this.bookmarkPop);
    }

    getChapter(id: number) {
        return this.chapters.find(t => t.id === id);
    }
}
