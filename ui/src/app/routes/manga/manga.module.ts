import { CommonModule } from "@angular/common";
import { NgModule } from "@angular/core";
import { FormsModule, ReactiveFormsModule } from "@angular/forms";
import { RouterModule, Routes } from "@angular/router";
import { COMMON_IMPORTS, ComponentsModule } from "src/app/components/components.module";
import { MangaPageComponent } from "./manga-page/manga-page.component";
import { MangaComponent } from './manga/manga.component';
import { MangaSelectorComponent } from './manga-selector/manga-selector.component';
import { MangaInProgressComponent } from './manga-in-progress/manga-in-progress.component';
import { MangaAddComponent } from './components/manga-add/manga-add.component';
import { MangaAddRouteComponent } from './manga-add-route/manga-add-route.component';
import { MangaCardComponent } from './components/manga-card/manga-card.component';
import { MangaBookmarksComponent } from './components/manga-bookmarks/manga-bookmarks.component';
import { MangaSearchFiltersComponent } from './components/manga-search-filters/manga-search-filters.component';
import { MangaStripMakerComponent } from './manga-strip-maker/manga-strip-maker.component';

const ROUTES: Routes = [
    {
        path: '',
        pathMatch: "full",
        redirectTo: 'all'
    }, {
        path: 'in-progress',
        pathMatch: 'full',
        redirectTo: 'touched/in-progress'
    },{
        path: 'touched',
        pathMatch: 'full',
        redirectTo: 'touched/in-progress'
    }, {
        path: 'all',
        component: MangaSelectorComponent
    }, {
        path: 'touched/:type',
        component: MangaInProgressComponent
    }, {
        path: 'add',
        component: MangaAddRouteComponent
    }, { 
        path: ':id',
        component: MangaComponent
    }, {
        path: ':id/:chapter/:page',
        component: MangaPageComponent
    }, {
        path: 'strip/:id/:chapter/:page',
        component: MangaStripMakerComponent
    }
];

const IMPORTS = [
    CommonModule,
    FormsModule,
    RouterModule.forChild(ROUTES),
    ReactiveFormsModule,
    ComponentsModule,
    ...COMMON_IMPORTS
];

@NgModule({
    declarations: [
        MangaPageComponent,
        MangaComponent,
        MangaSelectorComponent,
        MangaInProgressComponent,
        MangaAddComponent,
        MangaAddRouteComponent,
        MangaCardComponent,
        MangaBookmarksComponent,
        MangaSearchFiltersComponent,
        MangaStripMakerComponent,
    ],
    imports: [ ...IMPORTS ]
})
export class MangaModule { }