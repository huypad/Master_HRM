import { Component } from '@angular/core';
import { Router } from '@angular/router';
@Component({
  selector: 'app-error-403',
  standalone: true,
  templateUrl: './error-403.component.html'
})
export class Error403Component {
    constructor(private router: Router) {} 
  goBack() {
    this.router.navigate(['/home']);
}
}