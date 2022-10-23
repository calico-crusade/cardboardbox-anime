import { CommonModule } from "@angular/common";
import { NgModule } from "@angular/core";
import { FormsModule, ReactiveFormsModule } from "@angular/forms";
import { RouterModule, Routes } from "@angular/router";
import { COMMON_IMPORTS, ComponentsModule } from "src/app/components/components.module";
import { AdminGuard } from "src/app/services/admin.guard";
import { AiRequestsComponent } from "./ai-requests/ai-requests.component";
import { AiAllImagesComponent } from "./ai-all-images/ai-all-images.component";
import { AiComponent } from "./ai/ai.component";

const ROUTES: Routes = [
    { 
        path: '',
        pathMatch: 'full',
        component: AiComponent
    }, {
        path: 'requests',
        component: AiRequestsComponent
    }, {
        path: 'admin',
        component: AiAllImagesComponent,
        canActivate: [ AdminGuard ]
    }
]

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
        AiComponent,
        AiRequestsComponent,
        AiAllImagesComponent
    ],
    imports: [ ...IMPORTS ]
})
export class AiModule { }