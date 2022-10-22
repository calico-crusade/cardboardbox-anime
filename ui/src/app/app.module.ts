import { CommonModule } from '@angular/common';
import { HttpClientModule } from '@angular/common/http';
import { NgModule } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { BrowserModule, HammerModule } from '@angular/platform-browser';

import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './layout/app.component';
import { ErrorComponent } from './routes/error/error.component';
import { LightnovelComponent } from './routes/lightnovel/lightnovel.component';
import { LightnovelsComponent } from './routes/lightnovels/lightnovels.component';
import { AdminComponent } from './routes/admin/admin.component';
import { COMMON_IMPORTS, ComponentsModule } from './components/components.module';
import { AiComponent } from './routes/ai/ai.component';

@NgModule({
    declarations: [
        AppComponent,
        ErrorComponent,
        LightnovelComponent,
        LightnovelsComponent,
        AdminComponent,
        AiComponent,
    ],
    imports: [
        ComponentsModule,
        BrowserModule,
        AppRoutingModule,
        HttpClientModule,
        FormsModule,
        CommonModule,
        HammerModule,
        ...COMMON_IMPORTS
    ],
    bootstrap: [AppComponent]
})
export class AppModule { }
