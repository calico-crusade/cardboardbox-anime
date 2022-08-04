import { Component, Input, OnInit, ViewEncapsulation } from '@angular/core';
import { Anime, VrvAnime } from 'src/app/services/anime.model';

@Component({
    selector: 'cba-card',
    templateUrl: './card.component.html',
    styleUrls: ['./card.component.scss']
})
export class CardComponent implements OnInit {

    @Input() anime!: Anime;
    
    get langs() {
        return this.anime
            .metadata
            .languageTypes
            .filter(t => t !== 'Unknown')
            .map(t => {
                switch(t) {
                    case 'Dubbed': return 'Dub';
                    case 'Subbed': return 'Sub';
                    default: return t;
                }
            });
    }

    constructor() { }

    ngOnInit(): void {

    }

    getImage() {
        for(let img of this.anime.images) {
            if (img.type !== 'poster') continue;
            if (!img.width || !img.height) continue;
            if (img.width < 100 || img.height < 200) continue;
            
            return img.source;
        }

        return '';
    }
}
