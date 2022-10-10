import { CommonModule } from "@angular/common";
import { NgModule } from "@angular/core";
import { FormsModule, ReactiveFormsModule } from "@angular/forms";
import { RouterModule, Routes } from "@angular/router";
import { COMMON_IMPORTS, ComponentsModule } from "src/app/components/components.module";
import { BookComponent } from "./book/book.component";
import { SeriesListComponent } from "./series-list/series-list.component";
import { SeriesComponent } from "./series/series.component";

const ROUTES: Routes = [
    {
        path: '',
        pathMatch: 'full',
        component: SeriesListComponent
    }, {
        path: ':id',
        component: SeriesComponent
    }, {
        path: ':id/book/:bookId',
        component: BookComponent
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
        SeriesListComponent,
        SeriesComponent,
        BookComponent
    ],
    imports: [ ...IMPORTS ]
})
export class NovelsModule { }