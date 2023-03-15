import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';

import { ChatGptComponent } from './chat-gpt.component';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { COMMON_IMPORTS, ComponentsModule } from 'src/app/components/components.module';
import { RouterModule, Routes } from '@angular/router';

const ROUTES: Routes = [
    { path: '', component: ChatGptComponent }
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
        ChatGptComponent
    ],
    imports: [
        ...IMPORTS
    ]
})
export class ChatGptModule { }
