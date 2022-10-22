import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { ErrorComponent } from './routes/error/error.component';
import { LightnovelComponent } from './routes/lightnovel/lightnovel.component';
import { LightnovelsComponent } from './routes/lightnovels/lightnovels.component';
import { AdminComponent } from './routes/admin/admin.component';
import { AdminGuard } from './services/admin.guard';
import { AiComponent } from './routes/ai/ai.component';
import { AiAdminComponent } from './routes/ai-admin/ai-admin.component';

const routes: Routes = [
    {
        path: 'anime',
        loadChildren: () => import('./routes/anime').then(t => t.AnimeModule)
    }, {
        path: 'ln/:id',
        component: LightnovelComponent
    }, {
        path: 'ln',
        component: LightnovelsComponent
    }, {
        path: 'series',
        loadChildren: () => import('./routes/novels').then(m => m.NovelsModule)
    }, {
        path: 'ai',
        component: AiComponent
    }, {
       path: 'admin',
       component: AdminComponent,
       canActivate: [ AdminGuard ] 
    }, {
       path: 'admin/ai',
       component: AiAdminComponent,
       canActivate: [ AdminGuard ] 
    }, {
        path: '',
        pathMatch: 'full',
        redirectTo: '/anime/all'
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
