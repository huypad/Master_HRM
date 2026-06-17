import { Component, Input } from '@angular/core';
import { DropdownMenu3Component } from '../../../dropdown-menus/dropdown-menu3/dropdown-menu3.component';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-lists-widget10',
  templateUrl: './lists-widget10.component.html',
  imports: [DropdownMenu3Component, CommonModule]
})
export class ListsWidget10Component {
  @Input() cssClass: any;
  constructor() { }
}
