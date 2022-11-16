import { Component, Input, OnInit } from '@angular/core';
import { MangaWithChapters } from 'src/app/services';

@Component({
  selector: 'cba-manga-bookmarks',
  templateUrl: './manga-bookmarks.component.html',
  styleUrls: ['./manga-bookmarks.component.scss']
})
export class MangaBookmarksComponent implements OnInit {

    @Input() data!: MangaWithChapters;

    constructor() { }

    ngOnInit(): void {
    }

    getChapter(id: number) {
        return this.data.chapters.find(t => t.id === id);
    }
}
