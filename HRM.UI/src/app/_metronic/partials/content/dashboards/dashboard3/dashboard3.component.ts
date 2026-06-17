import { Component, OnInit } from '@angular/core';
import { LayoutService } from '../../../../core';
import { ListsWidget10Component } from '../../widgets/lists/lists-widget10/lists-widget10.component';
import { ListsWidget14Component } from '../../widgets/lists/lists-widget14/lists-widget14.component';
import { ListsWidget8Component } from '../../widgets/lists/lists-widget8/lists-widget8.component';
import { BaseTablesWidget1Component } from '../../widgets/base-tables/base-tables-widget1/base-tables-widget1.component';
import { BaseTablesWidget6Component } from '../../widgets/base-tables/base-tables-widget6/base-tables-widget6.component';
import { BaseTablesWidget2Component } from '../../widgets/base-tables/base-tables-widget2/base-tables-widget2.component';
import { AdvanceTablesWidget9Component } from '../../widgets/advance-tables/advance-tables-widget9/advance-tables-widget9.component';
import { StatsWidget10Component } from '../../widgets/stats/stats-widget10/stats-widget10.component';
import { StatsWidget11Component } from '../../widgets/stats/stats-widget11/stats-widget11.component';
import { MixedWidget4Component } from '../../widgets/mixed/mixed-widget4/mixed-widget4.component';

@Component({
  selector: 'app-dashboard3',
  templateUrl: './dashboard3.component.html',
  imports: [ListsWidget10Component, BaseTablesWidget1Component, AdvanceTablesWidget9Component, ListsWidget14Component,
    StatsWidget10Component, StatsWidget11Component, MixedWidget4Component,BaseTablesWidget6Component, BaseTablesWidget2Component,
    ListsWidget8Component
  ]
})
export class Dashboard3Component implements OnInit {
  constructor() { }

  ngOnInit(): void { }
}
