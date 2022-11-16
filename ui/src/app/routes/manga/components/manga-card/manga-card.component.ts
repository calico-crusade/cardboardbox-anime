import { Component, OnInit, Input } from '@angular/core';
import { LightNovelService, Manga, MangaProgressData } from 'src/app/services';

@Component({
  selector: 'cba-manga-card',
  templateUrl: './manga-card.component.html',
  styleUrls: ['./manga-card.component.scss']
})
export class MangaCardComponent implements OnInit {

    @Input() data?: Manga | MangaProgressData;

    get manga() {
        if (!this.data) return undefined;
        if ('id' in this.data) return this.data;
        return this.data.manga;
    }

    get progress() {
        if (!this.data || 'id' in this.data) return undefined;
        return this.data.progress;
    }

    get stats() {
        if (!this.data || 'id' in this.data) return undefined;
        return this.data.stats;
    }

    get chapter() {
        if (!this.data || 'id' in this.data) return undefined;
        return this.data.chapter;
    }

    get icon() {
        if (this.stats?.favourite) return { text: 'star', fill: true };
        if (this.stats?.chapterProgress === 100) return { text: 'check_circle' };
        if (this.progress) return { text: 'collections_bookmark' };
        return undefined;
    }

    constructor(
        private lnApi: LightNovelService
    ) { }

    ngOnInit(): void {
    }

    proxy(url?: string) {
        if (!url) return '';
        return this.lnApi.corsFallback(url, 'manga-covers');
    }
}
