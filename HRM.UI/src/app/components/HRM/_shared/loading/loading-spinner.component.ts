import { Component } from '@angular/core';
import { LoadingService } from '../../../../services/loading.service';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { CommonModule } from '@angular/common';

@Component({
    selector: 'app-loading-spinner',
    imports: [MatProgressSpinnerModule, CommonModule],
    template: `
    <div class="overlay" *ngIf="loadingService.loading$ | async">
      <mat-progress-spinner 
        mode="indeterminate" 
        diameter="50"
        strokeWidth="5"
        color="primary">
      </mat-progress-spinner>
    </div>
  `,
    styles: [`
    .overlay {
      position: fixed;
      top: 0;
      left: 0;
      right: 0;
      bottom: 0;
      background: rgba(255,255,255,0.6);
      display: flex;
      align-items: center;
      justify-content: center;
      z-index: 9999;
    }
  `]
})
export class LoadingSpinnerComponent {
    constructor(public loadingService: LoadingService) { }
}
