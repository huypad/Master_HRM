import { Component, OnInit } from '@angular/core';
import { Widget3DropdownComponent } from './widget3-dropdown/widget3-dropdown.component';
import { DropdownMenu2Component } from '../../../dropdown-menus/dropdown-menu2/dropdown-menu2.component';

@Component({
  selector: 'app-lists-widget3',
  templateUrl: './lists-widget3.component.html',
  imports: [Widget3DropdownComponent, DropdownMenu2Component]
})
export class ListsWidget3Component implements OnInit {
  constructor() {}

  ngOnInit(): void {}
}
