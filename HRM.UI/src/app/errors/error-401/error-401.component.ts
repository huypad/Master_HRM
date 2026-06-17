import { Component } from '@angular/core';
import { Router } from '@angular/router';

@Component({
  selector: 'app-error-401',
  standalone: true,
  templateUrl: './error-401.component.html'
})
export class Error401Component {
  constructor(private router: Router) { } // inject Router

  goLogin() {
    this.router.navigate(['/login']);
  }
}
