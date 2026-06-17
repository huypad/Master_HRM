import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-mixed-widget12',
  templateUrl: './mixed-widget12.component.html',
  imports: [
    CommonModule, 
  ],

})
export class MixedWidget12Component {
  @Input() cssClass: any ='';
  constructor() { }
}
