import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-advance-tables-widget9',
  templateUrl: './advance-tables-widget9.component.html',
  imports: [
    CommonModule, 
  ],
})
export class AdvanceTablesWidget9Component {
  @Input() cssClass: any;

  constructor() { }
}
