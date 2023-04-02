import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';

import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { COMMON_IMPORTS, ComponentsModule } from 'src/app/components/components.module';
import { RouterModule, Routes } from '@angular/router';
import { LayoutComponent } from './layout/layout.component';
import { NewChatComponent } from './new-chat/new-chat.component';
import { ActiveChatComponent } from './active-chat/active-chat.component';

const ROUTES: Routes = [
    { 
        path: '', 
        component: LayoutComponent,
        children: [
            { 
                path: '',
                pathMatch: 'full',
                redirectTo: 'new'
            }, {
                path: 'new',
                component: NewChatComponent
            }, {
                path: ':id',
                component: ActiveChatComponent
            }
        ]
    }
];

const IMPORTS = [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    ComponentsModule,
    ...COMMON_IMPORTS,
    RouterModule.forChild(ROUTES)
]

@NgModule({
    declarations: [
        LayoutComponent,
        NewChatComponent,
        ActiveChatComponent
    ],
    imports: [
        ...IMPORTS
    ]
})
export class ChatGptModule { }
