import { Component, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { ActivatedRoute } from '@angular/router';
import { PopupComponent, PopupService } from './../../../components';
import { 
    AuthService, LightNovelService, MangaChapter, MangaProgress, 
    MangaProgressData, MangaService, SubscriptionHandler
} from './../../../services';
import { MangaPartial } from '../manga-data.partial';

@Component({
    templateUrl: './manga.component.html',
    styleUrls: ['./manga.component.scss']
})
export class MangaComponent extends MangaPartial implements OnInit, OnDestroy {

    private _subs = new SubscriptionHandler();

    @ViewChild('bookmarkspopup') bookmarkPop!: PopupComponent;

    loading: boolean = false;
    error?: string;
    id!: string;

    stats?: MangaProgressData;
    isRandom: boolean = false;
    detailsOpen: boolean = false;

    get currentChapter() {
        return this.chapters.find(t => t.id === this.progress?.mangaChapterId);
    }

    get progress() {
        return this.stats?.progress;
    }

    constructor(
        private route: ActivatedRoute,
        private api: MangaService,
        private lnApi: LightNovelService,
        private title: Title,
        private auth: AuthService,
        private pop: PopupService
    ) { super(auth); }

    ngOnInit(): void {
        this._subs
            .subscribe(this.route.params, t => {
                this.id = (t['id'] + '').toLowerCase();
                this.isRandom = this.id === 'random';
                this.process();
            })
            .subscribe(this.auth.onLogin, t => this.process());
    }

    ngOnDestroy(): void {
        this._subs.unsubscribe();
        this.title.setTitle(this.api.defaultTitle);
        this.auth.title = undefined;
    }

    private async process() {
        if (this.loading) return;

        this.loading = true;
        try {
            this.data = await this.getMangaData();
        } catch (error) {
            console.error('Error occurred while fetching manga', {
                error,
                id: this.id
            });
        }

        this.title.setTitle('CBA | ' + this.manga?.title);
        this.auth.title = this.manga?.title;

        if (!this.data ||
            this.progress?.mangaId === this.manga?.id || 
            !this.auth.currentUser) {
            this.loading = false;
            return;
        }

        try {
            this.stats = await this.api.mangaExtended(this.data.manga.id).promise;
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
        if (this.id.toLowerCase() === 'random') {
            return this.api.random().promise;
        }

        return this.api.manga(this.id).promise;
    }

    nextRandom() { this.process(); }

    async update() {
        if (!this.manga) return;

        try {
            this.loading = true;
            const data = await this.api.reload(this.manga).promise;
            if (!data) return;
            this.data = data;
            this.stats = await this.api.mangaExtended(this.manga.id).promise;
        }
        catch (err) {
            console.error('Error occurred with update', { err, manga: this.manga });
        }

        this.loading = false;
    }

    toggleFavourite() {
        if (!this.loggedIn || !this.manga) return;
        this.api
            .favourite(this.manga.id)
            .subscribe(t => {
                if (!this.data) return;
                this.data.favourite = t;
            });
    }

    proxy(url?: string) {
        if (!url) return '';
        return this.lnApi.corsFallback(url, 'manga-covers');
    }

    showBookmarks() { this.pop.show(this.bookmarkPop); }

    async resetProgress() {
        if (!this.manga) return;

        await this.api.resetProgress(this.manga?.id).promise;
        this.stats = await this.api.mangaExtended(this.manga.id).promise;
    }

    getGroup(chapter: MangaChapter) {
        return chapter.attributes.find(t => t.name === 'Scanlation Group')?.value;
    }
}
