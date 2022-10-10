import { CommonModule } from "@angular/common";
import { NgModule } from "@angular/core";
import { FormsModule, ReactiveFormsModule } from "@angular/forms";
import { RouterModule, Routes } from "@angular/router";
import { COMMON_IMPORTS, ComponentsModule } from "src/app/components/components.module";
import { AnimeComponent } from "./anime/anime.component";
import { ListsComponent } from "./lists/lists.component";
import { PublicListsComponent } from "./public-lists/public-lists.component";

const ROUTES: Routes = [
    { 
        path: '',
        pathMatch: 'full',
        redirectTo: 'all'
    }, {
       path: 'all',
       component: AnimeComponent 
    }, {
        path: 'all/:id',
        component: AnimeComponent
    }, {
        path: 'lists/mine',
        component: ListsComponent
    }, {
        path: 'lists/public',
        component: PublicListsComponent
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
        AnimeComponent,
        ListsComponent,
        PublicListsComponent
    ],
    imports: [ ...IMPORTS ]
})
export class AnimeModule { }