import { CommonModule } from '@angular/common';
import { HttpClientModule } from '@angular/common/http';
import { NgModule } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { BrowserModule, HammerModule } from '@angular/platform-browser';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';

import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './layout/app.component';
import { ErrorComponent } from './routes/error/error.component';
import { AdminComponent } from './routes/admin/admin.component';
import { COMMON_IMPORTS, ComponentsModule } from './components/components.module';
import { ServiceWorkerModule } from '@angular/service-worker';
import { environment } from '../environments/environment';
import { RerouteComponent } from './routes/reroute/reroute.component';
import { TestComponent } from './routes/test/test.component';
import { MagicCircleModule } from './components/magic-circle';
import { InstallInstructionsComponent } from './routes/install-instructions/install-instructions.component';
import { CallbackComponent } from './routes/callback/callback.component';

@NgModule({
    declarations: [
        AppComponent,
        ErrorComponent,
        AdminComponent,
        RerouteComponent,
        TestComponent,
        InstallInstructionsComponent,
        CallbackComponent,
    ],
    imports: [
        ComponentsModule,
        BrowserModule,
        BrowserAnimationsModule,
        AppRoutingModule,
        HttpClientModule,
        FormsModule,
        CommonModule,
        HammerModule,
        MagicCircleModule,
        ...COMMON_IMPORTS,
        ServiceWorkerModule.register('ngsw-worker.js', {
          enabled: environment.production,
          // Register the ServiceWorker as soon as the application is stable
          // or after 30 seconds (whichever comes first).
          registrationStrategy: 'registerWhenStable:30000'
        })
    ],
    bootstrap: [AppComponent]
})
export class AppModule { }
