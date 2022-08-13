import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { Anime, Image } from 'src/app/services/anime.model';
import { AnimeModalService } from '../anime-modal/anime-modal.component';

@Component({
    selector: 'cba-card',
    templateUrl: './card.component.html',
    styleUrls: ['./card.component.scss']
})
export class CardComponent implements OnInit {

    /**
     * The anime the card is for
     */
    @Input() anime!: Anime;

    /**
     * Capture swipe events within the card
     */
    @Output('subswipe') onSwipe: EventEmitter<boolean> = new EventEmitter();
    
    /** 
     * Converts @see {@link Anime.languageTypes} from "Dubbed/Subbed" to "Dub/Sub" and removes "Unknown" 
     */
    get langs() {
        return [
            this.anime.languageTypes,
            ...this.anime.otherPlatforms.map(t => t.languageTypes)
        ]
        .flat()
        .filter((v, i, s) => s.indexOf(v) === i)
        .filter(t => t !== 'Unknown')
        .map(t => {
            switch(t) {
                case 'Dubbed': return 'Dub';
                case 'Subbed': return 'Sub';
                default: return t;
            }
        })
        .sort();
    }

    constructor(
        private srv: AnimeModalService
    ) { }

    ngOnInit(): void { }

    /**
     * Gets the anime's poster image
     * @returns The image source
     */
    getImage() {
        for(let img of this.anime.images) {
            //We only want posters
            if (img.type !== 'poster') continue;

            //Specifically get the first poster for hidive (since they only have wallpapers)
            if (this.anime.platformId === 'hidive') return img.source;

            //Ensure the image size
            if (!img.width || !img.height) continue;
            if (img.width < 100 || img.height < 200) continue;
            
            //We have specific logic for funimation images, so baily anything else out
            if (this.anime.platformId !== 'funimation') return img.source;

            //Handle the funimation image link
            return this.handleFunimationImage(img);
        }

        return '';
    }

    /**
     * Handles formatting funimation images with capping data
     * @param img The image we're transforming
     * @returns The modified image url
     */
    handleFunimationImage(img: Image) {
        //generate the modifier
        const getModifier = (w: number, h: number) => `c_fill,q_80,w_${w},h_${h}`;
        const width = 150, height = 210;
        const mod = getModifier(width, height);
        //Split the image by our known good url parts
        const parts = img.source.split('/image/upload/oth');
        if (parts.length === 1) return img.source;
        //Get the domain part of the url
        const dom = parts[0];
        //Cut the domain off remaining url
        parts.splice(0, 1);
        //string the url back together
        return dom + '/image/upload/' + mod + '/oth' + parts.join('/image/upload/oth');
    }

    /**
     * Opens the anime modal
     * @see {@link AnimeModalComponent}
     */
    modal() { this.srv.openAnime(this.anime); }

    /**
     * Capture swipe events within the card
     * @param left Whether or not the person is swiping left (true) or right (false)
     */
    doSwipe(left: boolean) { this.onSwipe.emit(left); }
}
