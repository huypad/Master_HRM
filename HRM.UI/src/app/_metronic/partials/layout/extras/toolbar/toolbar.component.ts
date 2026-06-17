import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { LayoutUtilsService } from '../../../../../components/HRM/_core/utils/layout-utils.service';

@Component({
  selector: 'app-toolbar',
  templateUrl: './toolbar.component.html',
  styleUrls: ['./toolbar.component.scss'],
  providers: [LayoutUtilsService],
})
export class ToolbarComponent implements OnInit {
  constructor(private router: Router) {}

  ngOnInit(): void {}

  navigateToBuilder() {
    this.router.navigate(['/builder']);
  }
}
