import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-tiles-widget10',
  templateUrl: './tiles-widget10.component.html',
  imports: [
    CommonModule, 
  ],

})
export class TilesWidget10Component {
  @Input() cssClass = '';
  @Input() widgetHeight = '130px';

  constructor() { }
}
