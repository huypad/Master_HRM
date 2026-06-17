import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-mixed-widget11',
  templateUrl: './mixed-widget11.component.html',
  imports: [
    CommonModule, 
  ],
})
export class MixedWidget11Component {
  @Input() cssClass: any = '';
  constructor() { }
}
