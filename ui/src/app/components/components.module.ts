import { CommonModule } from "@angular/common";
import { HttpClientModule, HTTP_INTERCEPTORS } from '@angular/common/http';
import { NgModule, Injectable } from "@angular/core";
import { FormsModule, ReactiveFormsModule } from "@angular/forms";
import { HAMMER_GESTURE_CONFIG, HammerGestureConfig, HammerModule } from '@angular/platform-browser';
import { LazyLoadImageModule } from "ng-lazyload-image";
import { InfiniteScrollModule } from "ngx-infinite-scroll";

import { AnimeModalComponent } from "./anime-modal/anime-modal.component";
import { CardComponent } from "./card/card.component";
import { ComponentContainerComponent } from "./component-container/component-container.component";
import { IconComponent } from "./icon/icon.component";
import { ListSelectComponent } from "./list-select/list-select.component";
import { ObserveDirective } from "./observe.directive";
import { SearchFiltersComponent } from "./search-filters/search-filters.component";

import * as Hammer from 'hammerjs';
import { AuthInterceptor } from "../services";
import { ImageFallbackDirective } from "./image-fallback.directive";
import { DeferLoadModule } from '@trademe/ng-defer-load';
import { PopupComponent } from './popup/popup.component';
import { ImageProxyDirective } from "./image-proxy.directive";
import { MarkdownDirective } from "./markdown.directive";
import { MagicCircleModule } from "./magic-circle";

@Injectable()
export class MyHammerConfig extends HammerGestureConfig {
    override overrides = {
        swipe: { direction: Hammer.DIRECTION_ALL }
    }
}

const EXPORTS = [
    SearchFiltersComponent,
    ListSelectComponent,
    IconComponent,
    ComponentContainerComponent,
    CardComponent,
    AnimeModalComponent,
    ObserveDirective,
    ImageFallbackDirective,
    ImageProxyDirective,
    PopupComponent,
    MarkdownDirective
];

export const COMMON_IMPORTS = [
    LazyLoadImageModule,
    InfiniteScrollModule,
    DeferLoadModule
]

@NgModule({
    declarations: [ ...EXPORTS  ],
    exports: [ ...EXPORTS ],
    imports: [
        CommonModule,
        ReactiveFormsModule,
        FormsModule,
        HttpClientModule,
        HammerModule,
        MagicCircleModule,
        ...COMMON_IMPORTS
    ], 
    providers: [
        {
            provide: HAMMER_GESTURE_CONFIG,
            useClass: MyHammerConfig
        }, {
            provide: HTTP_INTERCEPTORS,
            useClass: AuthInterceptor,
            multi: true
        }
    ]
})
export class ComponentsModule { }