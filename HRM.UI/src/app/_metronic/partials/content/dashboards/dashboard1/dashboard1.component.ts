import { Component, OnInit } from '@angular/core';
import { LayoutService } from '../../../../core';
import { MixedWidget1Component } from '../../widgets/mixed/mixed-widget1/mixed-widget1.component';
import { MixedWidget14Component } from '../../widgets/mixed/mixed-widget14/mixed-widget14.component';
import { ListsWidget1Component } from '../../widgets/lists/lists-widget1/lists-widget1.component';
import { ListsWidget3Component } from '../../widgets/lists/lists-widget3/lists-widget3.component';
import { ListsWidget4Component } from '../../widgets/lists/lists-widget4/lists-widget4.component';
import { ListsWidget8Component } from '../../widgets/lists/lists-widget8/lists-widget8.component';
import { ListsWidget9Component } from '../../widgets/lists/lists-widget9/lists-widget9.component';
import { StatsWidget11Component } from '../../widgets/stats/stats-widget11/stats-widget11.component';
import { StatsWidget12Component } from '../../widgets/stats/stats-widget12/stats-widget12.component';
import { AdvanceTablesWidget2Component } from '../../widgets/advance-tables/advance-tables-widget2/advance-tables-widget2.component';
import { AdvanceTablesWidget4Component } from '../../widgets/advance-tables/advance-tables-widget4/advance-tables-widget4.component';

@Component({
  selector: 'app-dashboard1',
  templateUrl: './dashboard1.component.html',
  imports: [MixedWidget1Component, MixedWidget14Component, ListsWidget1Component, ListsWidget3Component,
    ListsWidget4Component, ListsWidget8Component, ListsWidget9Component, StatsWidget11Component, StatsWidget12Component,
    AdvanceTablesWidget2Component, AdvanceTablesWidget4Component
  ]
})
export class Dashboard1Component implements OnInit {
  constructor() {}

  ngOnInit(): void {}
}
