import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common'

@Component({
  selector: 'app-advance-tables-widget1',
  templateUrl: './advance-tables-widget1.component.html',
  imports: [
    CommonModule, 
  ],
})
export class AdvanceTablesWidget1Component {
  @Input() cssClass: any = '';
  constructor() { }
}
