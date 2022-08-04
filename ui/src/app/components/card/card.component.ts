import { Component, Input, OnInit, ViewEncapsulation } from '@angular/core';
import { VrvAnime } from 'src/app/services/anime.model';

@Component({
    selector: 'cba-card',
    templateUrl: './card.component.html',
    styleUrls: ['./card.component.scss']
})
export class CardComponent implements OnInit {

    @Input() anime!: VrvAnime;

    constructor() { }

    ngOnInit(): void {

    }

    getImage() {
        for(let img of this.anime.images) {
            if (img.type !== 'poster_tall') continue;

            if (img.width < 100 || img.height < 200) continue;
            
            return img.source;
        }

        return '';
    }
}
