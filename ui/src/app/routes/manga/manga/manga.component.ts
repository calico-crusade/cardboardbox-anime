import { Component, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { ActivatedRoute } from '@angular/router';
import { PopupComponent, PopupService } from 'src/app/components';
import { AuthService, LightNovelService, MangaProgress, MangaService, MangaWithChapters, SubscriptionHandler } from 'src/app/services';

@Component({
    templateUrl: './manga.component.html',
    styleUrls: ['./manga.component.scss']
})
export class MangaComponent implements OnInit, OnDestroy {

    private _subs = new SubscriptionHandler();

    @ViewChild('bookmarkspopup') bookmarkPop!: PopupComponent;

    loading: boolean = false;
    error?: string;
    id!: number;
    data?: MangaWithChapters;
    progress?: MangaProgress;
    isRandom: boolean = false;

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
        this._subs
            .subscribe(this.route.params, t => {
                const id = t['id'] + '';
                this.id = id.toLowerCase() === 'random' ? -1 : +id;
                this.isRandom = this.id === -1;
                this.process();
            })
            .subscribe(this.auth.onLogin, t => {
                this.process();
            });
    }

    ngOnDestroy(): void {
        this._subs.unsubscribe();
        this.title.setTitle(this.api.defaultTitle);
        this.auth.title = undefined;
    }

    private async process() {
        this.loading = true;
        try {
            if (this.data?.manga.id !== this.id)
                this.data = await this.getMangaData();
            this.id = this.data?.manga.id;
        } catch (error) {
            console.error('Error occurred while fetching manga', {
                error,
                id: this.id
            });
        }

        this.title.setTitle('CBA | ' + this.manga?.title);
        this.auth.title = this.manga?.title;

        if (!this.data ||
            this.progress?.mangaId === this.id || 
            !this.auth.currentUser) {
            this.loading = false;
            return;
        }

        try {
            this.progress = await this.api.progress(this.data.manga.id).promise;
        } catch (error) {
            console.error('Error occurred while fetching manga progress', {
                error,
                id: this.id,
                data: this.data
            });
        }

        this.loading = false;
    }

    private getMangaData() {
        if (this.id <= 0) {
            return this.api.random().promise;
        }

        return this.api.manga(this.id).promise;
    }

    nextRandom() {
        this.id = -1;
        this.process();
    }

    update() {
        if (!this.manga) return;

        this.loading = true;
        this.api
            .reload(this.manga)
            .error(err => this.error = 'An error occurred while refreshing the manga!')
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
}
