import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AnimeComponent } from './routes/anime/anime.component';
import { ErrorComponent } from './routes/error/error.component';

const routes: Routes = [
    {
        path: 'anime',
        component: AnimeComponent
    }, {
        path: 'anime/:id',
        component: AnimeComponent
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
