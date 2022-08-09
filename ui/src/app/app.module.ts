import { CommonModule } from '@angular/common';
import { HttpClientModule, HTTP_INTERCEPTORS } from '@angular/common/http';
import { Injectable, NgModule } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { BrowserModule, HAMMER_GESTURE_CONFIG, HammerGestureConfig, HammerModule } from '@angular/platform-browser';

import * as Hammer from 'hammerjs';

import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './layout/app.component';
import { AnimeComponent } from './routes/anime/anime.component';
import { ErrorComponent } from './routes/error/error.component';
import { CardComponent } from './components/card/card.component';
import { LazyLoadImageModule } from 'ng-lazyload-image';
import { InfiniteScrollModule } from 'ngx-infinite-scroll';
import { SearchFiltersComponent } from './components/search-filters/search-filters.component';
import { AnimeModalComponent } from './components/anime-modal/anime-modal.component';
import { AuthInterceptor } from './services/auth.service';
import { ListSelectComponent } from './components/list-select/list-select.component';

@Injectable()
export class MyHammerConfig extends HammerGestureConfig {

    override overrides = {
        swipe: { direction: Hammer.DIRECTION_ALL }
    }
}

@NgModule({
    declarations: [
        AppComponent,
        AnimeComponent,
        ErrorComponent,
        CardComponent,
        SearchFiltersComponent,
        AnimeModalComponent,
        ListSelectComponent
    ],
    imports: [
        BrowserModule,
        AppRoutingModule,
        HttpClientModule,
        FormsModule,
        CommonModule,
        LazyLoadImageModule,
        HammerModule,
        InfiniteScrollModule
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
    ],
    bootstrap: [AppComponent]
})
export class AppModule { }
