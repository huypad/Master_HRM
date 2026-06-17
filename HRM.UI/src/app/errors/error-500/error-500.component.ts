import { Component } from '@angular/core';
import { Router } from '@angular/router';

@Component({
    selector: 'app-error-500',
    standalone: true,
    templateUrl: './error-500.component.html'
})
export class Error500Component {
    constructor(private router: Router) { }
    goBack() {
        this.router.navigate(['/home']);
    }
}