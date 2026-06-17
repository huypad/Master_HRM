import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-mixed-widget10',
  templateUrl: './mixed-widget10.component.html',
  imports: [
    CommonModule, 
  ],
})
export class MixedWidget10Component {
  @Input() cssClass: any = '';
  constructor() { }
}
