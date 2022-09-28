import { AfterViewInit, Component, ElementRef, OnDestroy, OnInit, QueryList, ViewChild, ViewChildren } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { ActivatedRoute, Router } from '@angular/router';
import { AnimeService, Chapter } from './../../services';

@Component({
    templateUrl: './lightnovel.component.html',
    styleUrls: ['./lightnovel.component.scss']
})
export class LightnovelComponent implements OnInit, OnDestroy, AfterViewInit {

    id!: string;
    page: number = 1;
    size: number = 100;
    loading: boolean = false;
    open: boolean = true;

    chapters: Chapter[] = [];
    pages: number = 0;
    count: number = 0;

    inView: number = 0;

    @ViewChildren('chapter') 
    chapterEls!: QueryList<ElementRef>;

    @ViewChild('chapterScroll')
    main!: ElementRef;

    constructor(
        private route: ActivatedRoute,
        private router: Router,
        private api: AnimeService,
        private title: Title
    ) { }

    ngAfterViewInit() {
        const el = this.main?.nativeElement;
        if (!el) {
            console.error('Intersection observer target element is null!');
            return;
        }

        const obsv = new IntersectionObserver((els) => {
            console.log('Intersection Observer', {
                els
            });
        });

        obsv.observe(el);
        console.log('Intersection Observer Target', {
            el
        });
    }

    ngOnDestroy() {
        
    }

    ngOnInit(): void {
        this.route.params.subscribe(t => {
            this.id = t['id'];
            this.chapters = [];
            this.process();
        });


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

                if (this.chapters.length > 0)
                    this.title.setTitle('CBA - ' + this.chapters[0].book);
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

        el.nativeElement.scrollIntoView({behavior: "smooth", block: "start", inline: "nearest"});
    }

    onVisible(index: number) {
        this.inView = index;
    }
}
