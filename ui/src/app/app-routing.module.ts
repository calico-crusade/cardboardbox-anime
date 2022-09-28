import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AnimeComponent } from './routes/anime/anime.component';
import { ErrorComponent } from './routes/error/error.component';
import { LightnovelComponent } from './routes/lightnovel/lightnovel.component';
import { LightnovelsComponent } from './routes/lightnovels/lightnovels.component';
import { ListsComponent } from './routes/lists/lists.component';
import { PublicListsComponent } from './routes/public-lists/public-lists.component';

const routes: Routes = [
    {
        path: 'anime',
        component: AnimeComponent
    }, {
        path: 'anime/:id',
        component: AnimeComponent
    }, {
        path: 'lists',
        component: ListsComponent
    }, {
        path: 'public-lists',
        component: PublicListsComponent
    }, {
        path: 'ln/:id',
        component: LightnovelComponent
    }, {
        path: 'ln',
        component: LightnovelsComponent
    }, {
        path: '',
        pathMatch: 'full',
        redirectTo: '/anime'
    }, {
        path: '**',
        component: ErrorComponent
    }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
