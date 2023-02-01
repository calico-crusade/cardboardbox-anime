import { AuthService, StorageVar } from "./services";

export abstract class SettingsPartial {

    private _mangaInvertControls = new StorageVar<boolean>(false, 'invert-controls');
    private _mangaImgSize = new StorageVar<string>('Fit to Height', 'img-size');
    private _mangaScroll = new StorageVar<boolean>(false, 'scroll-chapter');
    private _mangaHideHeader = new StorageVar<boolean>(true, 'hide-header', (v) => this._auth.showHeader = !v);
    private _mangaInvert = new StorageVar<boolean>(false, 'invert-image');
    private _mangaScrollAmount = new StorageVar<number>(100, 'scroll-amount');
    private _mangaProgressBar = new StorageVar<string>('', 'progress-bar');
    private _mangaNoDirectionalButton = new StorageVar<boolean>(false, 'no-directional-buttons');
    private _mangaHideExtraButtons = new StorageVar<boolean>(false, 'hide-extra-buttons');
    private _mangaFilter = new StorageVar<string>('blue-light', 'filter');
    private _mangaCustomFilter = new StorageVar<string>('sepia(40%) saturate(200%)', 'custom-filter', (v) => this.setRootVar('--custom-image-filter', '', v));
    private _mangaBrightness = new StorageVar<number>(70, 'manga-brightness', (v) => this.setRootVar('--image-bightness', '100%', v ? v + '%' : '100%'));
    private _mangaShowNsfw = new StorageVar<boolean>(false, 'manga-show-nsfw');
    private _mangaTab = new StorageVar<string>('info', 'default-tab');

    private _lnFontSize = new StorageVar<number>(16, 'ln-font-size');
    private _lnPageSize = new StorageVar<number>(600, 'ln-page-size');
    private _lnDisableDic = new StorageVar<boolean>(false, 'ln-disable-dic');

    get mangaInvertControls(): boolean { return this._mangaInvertControls.value; }
    set mangaInvertControls(value: boolean | undefined) { this._mangaInvertControls.value = value; }
    
    get mangaImgSize(): string { return this._mangaImgSize.value; }
    set mangaImgSize(value: string | undefined) { this._mangaImgSize.value = value; }
    
    get mangaScroll(): boolean { return this._mangaScroll.value; }
    set mangaScroll(value: boolean | undefined) { this._mangaScroll.value = value; }
    
    get mangaHideHeader(): boolean { return this._mangaHideHeader.value; }
    set mangaHideHeader(value: boolean | undefined) { this._mangaHideHeader.value = value; }
    
    get mangaInvert(): boolean { return this._mangaInvert.value; }
    set mangaInvert(value: boolean | undefined) { this._mangaInvert.value = value; }
    
    get mangaScrollAmount(): number { return this._mangaScrollAmount.value; }
    set mangaScrollAmount(value: number | undefined) { this._mangaScrollAmount.value = value; }
    
    get mangaProgressBar(): string { return this._mangaProgressBar.value; }
    set mangaProgressBar(value: string | undefined) { this._mangaProgressBar.value = value; }
    
    get mangaNoDirectionalButton(): boolean { return this._mangaNoDirectionalButton.value; }
    set mangaNoDirectionalButton(value: boolean | undefined) { this._mangaNoDirectionalButton.value = value; }
    
    get mangaHideExtraButtons(): boolean { return this._mangaHideExtraButtons.value; }
    set mangaHideExtraButtons(value: boolean | undefined) { this._mangaHideExtraButtons.value = value; }
    
    get mangaFilter(): string { return this._mangaFilter.value; }
    set mangaFilter(value: string | undefined) { this._mangaFilter.value = value; }
    
    get mangaCustomFilter(): string { return this._mangaCustomFilter.value; }
    set mangaCustomFilter(value: string | undefined) { this._mangaCustomFilter.value = value; }
    
    get mangaBrightness(): number { return this._mangaBrightness.value; }
    set mangaBrightness(value: number | undefined) { this._mangaBrightness.value = value; }

    get mangaShowNsfw(): boolean { return this._mangaShowNsfw.value; }
    set mangaShowNsfw(value: boolean | undefined) { this._mangaShowNsfw.value = value; }

    get mangaTab(): string { return this._mangaTab.value; }
    set mangaTab(value: string) { this._mangaTab.value = value; }

    get lnFontSize(): number { return this._lnFontSize.value; }
    set lnFontSize(value: number) { this._lnFontSize.value = value; }

    get lnPageSize(): number { return this._lnPageSize.value; }
    set lnPageSize(value: number) { this._lnPageSize.value = value; }

    get lnEnableDic(): boolean { return !this._lnDisableDic.value; }
    set lnEnableDic(value: boolean) { this._lnDisableDic.value = !value; }

    get loggedIn() { return !!this._auth.currentUser; }
    get fitToWidth() { return this.mangaImgSize === 'Fit to Width'; }
    get fitToHeight() { return this.mangaImgSize === 'Fit to Height'; }

    get mangaSettings() {
        return [
            this._mangaInvertControls, this._mangaImgSize, this._mangaScroll,
            this._mangaHideHeader, this._mangaInvert, this._mangaScrollAmount,
            this._mangaProgressBar, this._mangaNoDirectionalButton, this._mangaHideExtraButtons,
            this._mangaFilter, this._mangaCustomFilter, this._mangaBrightness
        ]
    }

    constructor(
        private _auth: AuthService
    ) { 
        this.mangaCustomFilter = this.mangaCustomFilter;
        this.mangaBrightness = this.mangaBrightness;
    }

    initManga() { this._auth.showHeader = !this.mangaHideHeader; }

    setRootVar(name: string, def: string, value?: string) {
        document.documentElement.style.setProperty(name, value || def);
    }
}