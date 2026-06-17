import { Component, OnInit } from '@angular/core';
import { Widget4DropdownComponent } from './widget4-dropdown/widget4-dropdown.component';
import { DropdownMenu2Component } from '../../../dropdown-menus/dropdown-menu2/dropdown-menu2.component';

@Component({
  selector: 'app-lists-widget4',
  templateUrl: './lists-widget4.component.html',
  imports:[Widget4DropdownComponent, DropdownMenu2Component]
})
export class ListsWidget4Component implements OnInit {
  constructor() {}

  ngOnInit(): void {}
}
