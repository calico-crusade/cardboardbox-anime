import { CommonModule } from "@angular/common";
import { NgModule } from "@angular/core";
import { FormsModule, ReactiveFormsModule } from "@angular/forms";
import { RouterModule, Routes } from "@angular/router";
import { COMMON_IMPORTS, ComponentsModule } from "src/app/components/components.module";
import { MangaPageComponent } from "./manga-page/manga-page.component";
import { MangaComponent } from './manga/manga.component';
import { MangaSelectorComponent } from './manga-selector/manga-selector.component';
import { MangaInProgressComponent } from './manga-in-progress/manga-in-progress.component';

const ROUTES: Routes = [
    {
        path: '',
        pathMatch: "full",
        component: MangaSelectorComponent
    }, {
        path: 'in-progress',
        component: MangaInProgressComponent
    }, { 
        path: ':id',
        component: MangaComponent
    }, {
        path: ':id/:chapter/:page',
        component: MangaPageComponent
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
        MangaInProgressComponent
    ],
    imports: [ ...IMPORTS ]
})
export class MangaModule { }