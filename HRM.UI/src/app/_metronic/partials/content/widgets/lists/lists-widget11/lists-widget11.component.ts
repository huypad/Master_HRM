import { Component, Input, OnInit } from '@angular/core';
import { DropdownMenu4Component } from '../../../dropdown-menus/dropdown-menu4/dropdown-menu4.component';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-lists-widget11',
  templateUrl: './lists-widget11.component.html',
  imports: [DropdownMenu4Component, CommonModule]
})
export class ListsWidget11Component implements OnInit {
  @Input() cssClass: any ='';

  constructor() { }

  ngOnInit(): void { }
}
