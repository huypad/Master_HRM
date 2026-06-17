import { Component, OnInit, Input } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

export interface SearchItem {
  type: number; // 0 for Documents, 1 for Members, 2 for Files
  items: {
    title: string;
    description: string;
    svgPath?: string; // Optional, used in documentsTemplate
    imgPath?: string; // Optional, used in membersTemplate
    iconClasses?: string; // Optional, used in filesTemplate
  }[];
}

@Component({
  selector: 'app-search-result',
  templateUrl: './search-result.component.html',
  styleUrls: ['./search-result.component.scss'],
  standalone: true,
  imports: [
    ReactiveFormsModule,
    FormsModule,
    CommonModule, 
    ReactiveFormsModule,RouterModule
  ],
})


export class SearchResultComponent implements OnInit {
  // Public properties
  @Input() data: SearchItem[] | null = null;
  constructor() {}

  ngOnInit(): void {}
}
