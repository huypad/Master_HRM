import { Component, OnInit } from '@angular/core';
import { DropdownMenu2Component } from '../../../../dropdown-menus/dropdown-menu2/dropdown-menu2.component';

@Component({
  selector: 'app-widget4-dropdown',
  templateUrl: './widget4-dropdown.component.html',
  imports: [DropdownMenu2Component]
})
export class Widget4DropdownComponent implements OnInit {
  constructor() {}

  ngOnInit(): void {}
}
