import { Component } from '@angular/core';
import { RouterModule } from '@angular/router';
import { LoadingSpinnerComponent } from './components/HRM/_shared/loading/loading-spinner.component';

@Component({
  selector: 'app-root',
  standalone: true,
  templateUrl: './app.component.html',
  imports: [RouterModule, LoadingSpinnerComponent],
})
export class AppComponent { }