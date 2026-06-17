import { Component, OnInit } from '@angular/core';
import { DropdownMenu4Component } from '../../../dropdown-menus/dropdown-menu4/dropdown-menu4.component';

@Component({
  selector: 'app-lists-widget1',
  templateUrl: './lists-widget1.component.html',
  imports:[DropdownMenu4Component]
})
export class ListsWidget1Component implements OnInit {
  constructor() {}

  ngOnInit(): void {}
}
