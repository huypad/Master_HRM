import { bootstrapApplication } from '@angular/platform-browser';
import { AppComponent } from './app/app.component';
import { provideRouter, withDebugTracing } from '@angular/router';
import { routes } from './app/app.routes';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { AuthInterceptor } from './app/interceptors/auth.interceptor';
import '@angular/localize/init';
import { enableProdMode, importProvidersFrom } from '@angular/core';
import { environment } from './app/environments/environment';
import 'zone.js';
import { provideNativeDateAdapter } from '@angular/material/core'
import { DatePipe } from '@angular/common';
import { HttpErrorInterceptor } from './app/interceptors/http-error.interceptor';
import { GeneralService } from './app/components/HRM/_core/services/general.service';
import { HTTP_INTERCEPTORS } from '@angular/common/http';
import { LoadingInterceptor } from './app/interceptors/loading.interceptor';
if (environment.production) {
  enableProdMode();
}

bootstrapApplication(AppComponent, {
  providers: [
    provideRouter(routes, withDebugTracing()),
    provideHttpClient(withInterceptors([AuthInterceptor, HttpErrorInterceptor, LoadingInterceptor])),
    DatePipe, provideNativeDateAdapter(), GeneralService,
  ]
}).catch((err) => console.error(err));
