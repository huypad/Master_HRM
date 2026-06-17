import { Component, OnInit } from '@angular/core';
import { LayoutService } from '../../../../core';
import { Dashboard1Component } from '../dashboard1/dashboard1.component';
import { Dashboard2Component } from '../dashboard2/dashboard2.component';
import { Dashboard3Component } from '../dashboard3/dashboard3.component';
import { CommonModule } from '@angular/common';


@Component({
  selector: 'app-dashboard-wrapper',
  templateUrl: './dashboard-wrapper.component.html',
  imports: [Dashboard1Component, Dashboard2Component, Dashboard3Component, CommonModule]
})
export class DashboardWrapperComponent implements OnInit {
  demo!: string;
  constructor(private layout: LayoutService) {}

  ngOnInit(): void {
    this.demo = this.layout.getProp('demo');
  }
}
