import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-tiles-widget14',
  templateUrl: './tiles-widget14.component.html',
  imports: [
    CommonModule, 
  ],
})
export class TilesWidget14Component {
  @Input() cssClass = '';
  constructor() { }
}
