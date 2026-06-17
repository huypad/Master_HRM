import { Component, OnInit } from '@angular/core';
import { DropdownMenu1Component } from '../../../../dropdown-menus/dropdown-menu1/dropdown-menu1.component';

@Component({
  selector: 'app-widget3-dropdown',
  templateUrl: './widget3-dropdown.component.html',
  imports: [DropdownMenu1Component]
})
export class Widget3DropdownComponent implements OnInit {
  constructor() {}

  ngOnInit(): void {}
}
