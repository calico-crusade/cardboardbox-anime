import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { ErrorComponent } from './routes/error/error.component';
import { LightnovelComponent } from './routes/lightnovel/lightnovel.component';
import { LightnovelsComponent } from './routes/lightnovels/lightnovels.component';
import { AdminComponent } from './routes/admin/admin.component';
import { RerouteComponent } from './routes/reroute/reroute.component';
import { TestComponent } from './routes/test/test.component';
import { InstallInstructionsComponent } from './routes/install-instructions/install-instructions.component';

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
        loadChildren: () => import('./routes/ai').then(m => m.AiModule)
    }, {
        path: 'manga',
        loadChildren: () => import('./routes/manga').then(m => m.MangaModule)
    },{
       path: 'admin',
       component: AdminComponent
    }, {
       path: 'test',
       component: TestComponent 
    }, {
        path: 'install',
        pathMatch: 'full',
        redirectTo: 'install/windows'
    }, {
        path: 'install/:type',
        component: InstallInstructionsComponent
    }, {
        path: '',
        pathMatch: 'full',
        component: RerouteComponent
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
