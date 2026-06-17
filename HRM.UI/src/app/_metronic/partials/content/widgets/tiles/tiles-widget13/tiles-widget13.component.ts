import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-tiles-widget13',
  templateUrl: './tiles-widget13.component.html',
  imports: [
    CommonModule, 
  ],
})
export class TilesWidget13Component {
  @Input() cssClass = '';
  @Input() widgetHeight = '225px';

  constructor() { }
}
