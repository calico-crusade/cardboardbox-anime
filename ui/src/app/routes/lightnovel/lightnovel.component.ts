import { Component, ElementRef, OnDestroy, OnInit, QueryList, ViewChild, ViewChildren } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { ActivatedRoute } from '@angular/router';
import { AnimeService, AuthService, Chapter } from './../../services';

export type MENU_TYPE = 'chapters' | 'settings' | 'none';

@Component({
    templateUrl: './lightnovel.component.html',
    styleUrls: ['./lightnovel.component.scss']
})
export class LightnovelComponent implements OnInit, OnDestroy {

    private _fontSize?: number;

    id!: string;
    page: number = 1;
    size: number = 100;
    loading: boolean = false;
    menu: MENU_TYPE = 'none';
    chapters: Chapter[] = [];
    pages: number = 0;
    count: number = 0;
    inView: number = 0;

    get fontSize() {
        if (this._fontSize) return this._fontSize;
        const val = localStorage.getItem('ln-fontsize');
        if (!val) return 16;
        return this._fontSize = +val;
    }
    set fontSize(val: number) {
        this._fontSize = val;
        localStorage.setItem('ln-fontsize', val.toString());
    }

    @ViewChildren('chapter') chapterEls!: QueryList<ElementRef>;
    @ViewChildren('chapbtn') chapterBtnEls!: QueryList<ElementRef>;
    @ViewChild('chapterScroll') main!: ElementRef;

    constructor(
        private route: ActivatedRoute,
        private api: AnimeService,
        private title: Title,
        private auth: AuthService
    ) { }


    ngOnInit(): void {
        this.route.params.subscribe(t => {
            this.id = t['id'];
            this.chapters = [];
            this.process();
        });
    }

    ngOnDestroy(): void {
        this.title.setTitle('CardboardBox | Anime');
        this.auth.title = undefined;
    }

    private process() {
        this.loading = true;
        this.api.lightnovel(this.id, this.page, this.size)
            .subscribe(t => {
                const { count, pages, results } = t;
                this.chapters.push(...results);
                this.count = count;
                this.pages = pages;
                this.loading = false;

                if (this.chapters.length > 0) {
                    this.title.setTitle('CBA - ' + this.chapters[0].book);
                    this.auth.title = this.chapters[0].book;
                }
            });
    }

    onScroll() {
        this.page += 1;
        this.process();
    }

    jump(index: number) {
        const el = this.chapterEls.get(index);
        if (!el) {
            console.error(`Could not find el for: ${index}`);
            return;
        }

        el.nativeElement.scrollIntoView({behavior: "auto", block: "start", inline: "nearest"});
    }

    onVisible(index: number) {
        this.inView = index;

        const chap = this.chapterBtnEls.get(index);
        if (!chap) {
            console.warn(`Couldn't find chapter button for: ${index}`, { btns: this.chapterBtnEls });
            return;
        }

        chap.nativeElement.scrollIntoView({behavior: "smooth", block: "center", inline: "nearest"})
    }

    toggle(target: MENU_TYPE) {
        if (this.menu === target) {
            this.menu = 'none';
            return;
        }

        this.menu = target;
    }
}
