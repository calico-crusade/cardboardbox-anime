import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { RouterModule, Routes } from '@angular/router';
import { COMMON_IMPORTS, ComponentsModule } from '../../components/components.module';

import { DiscordSettingsComponent } from './discord-settings/discord-settings.component';

const ROUTES: Routes = [
    { path: '', component: DiscordSettingsComponent },
    { path: ':id', component: DiscordSettingsComponent },
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
        DiscordSettingsComponent
    ],
    imports: [
        ...IMPORTS
    ]
})
export class DiscordModule { }
