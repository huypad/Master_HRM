import { Component } from '@angular/core';
import { Router } from '@angular/router';

@Component({
  selector: 'app-error-404',
  standalone: true,
  templateUrl: './error-404.component.html'
})
export class Error404Component {
    constructor(private router: Router) {} 
  goBack() {
    this.router.navigate(['/home']);
}
}