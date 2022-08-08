import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { Anime, Image } from 'src/app/services/anime.model';
import { AnimeModalService } from '../anime-modal/anime-modal.component';

@Component({
    selector: 'cba-card',
    templateUrl: './card.component.html',
    styleUrls: ['./card.component.scss']
})
export class CardComponent implements OnInit {

    @Input() anime!: Anime;

    @Output('subswipe') onSwipe: EventEmitter<boolean> = new EventEmitter();
    
    get langs() {
        return this.anime
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

    constructor(
        private srv: AnimeModalService
    ) { }

    ngOnInit(): void {

    }

    getImage() {
        for(let img of this.anime.images) {
            if (img.type !== 'poster') continue;

            if (this.anime.platformId === 'hidive') return img.source;

            if (!img.width || !img.height) continue;
            if (img.width < 100 || img.height < 200) continue;
            
            if (this.anime.platformId !== 'funimation') return img.source;

            return this.handleFunimationImage(img);
        }

        return '';
    }

    handleFunimationImage(img: Image) {
        const getModifier = (w: number, h: number) => `c_fill,q_80,w_${w},h_${h}`;
        const width = 150, height = 210;
        const mod = getModifier(width, height);

        const parts = img.source.split('/image/upload/oth');
        if (parts.length === 1) return img.source;

        const dom = parts[0];
        parts.splice(0, 1);

        return dom + '/image/upload/' + mod + '/oth' + parts.join('/image/upload/oth');
    }

    modal() {
        this.srv.openAnime(this.anime);
    }

    doSwipe(left: boolean) {
        this.onSwipe.emit(left);
    }
}
