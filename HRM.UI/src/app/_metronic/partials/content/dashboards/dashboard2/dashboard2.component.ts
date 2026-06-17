import { Component, OnInit } from '@angular/core';
import { MixedWidget10Component } from '../../widgets/mixed/mixed-widget10/mixed-widget10.component';
import { MixedWidget11Component } from '../../widgets/mixed/mixed-widget11/mixed-widget11.component';
import { MixedWidget12Component } from '../../widgets/mixed/mixed-widget12/mixed-widget12.component';
import { ListsWidget10Component } from '../../widgets/lists/lists-widget10/lists-widget10.component';
import { ListsWidget1Component } from '../../widgets/lists/lists-widget1/lists-widget1.component';
import { ListsWidget3Component } from '../../widgets/lists/lists-widget3/lists-widget3.component';
import { ListsWidget4Component } from '../../widgets/lists/lists-widget4/lists-widget4.component';
import { ListsWidget8Component } from '../../widgets/lists/lists-widget8/lists-widget8.component';
import { ListsWidget9Component } from '../../widgets/lists/lists-widget9/lists-widget9.component';
import { ListsWidget11Component } from '../../widgets/lists/lists-widget11/lists-widget11.component';
import { TilesWidget3Component } from '../../widgets/tiles/tiles-widget3/tiles-widget3.component';
import { TilesWidget1Component } from '../../widgets/tiles/tiles-widget1/tiles-widget1.component';
import { TilesWidget10Component } from '../../widgets/tiles/tiles-widget10/tiles-widget10.component';
import { TilesWidget11Component } from '../../widgets/tiles/tiles-widget11/tiles-widget11.component';
import { TilesWidget12Component } from '../../widgets/tiles/tiles-widget12/tiles-widget12.component';
import { TilesWidget13Component } from '../../widgets/tiles/tiles-widget13/tiles-widget13.component';
import { TilesWidget14Component } from '../../widgets/tiles/tiles-widget14/tiles-widget14.component';
import { StatsWidget11Component } from '../../widgets/stats/stats-widget11/stats-widget11.component';
import { StatsWidget12Component } from '../../widgets/stats/stats-widget12/stats-widget12.component';
import { MixedWidget1Component } from '../../widgets/mixed/mixed-widget1/mixed-widget1.component';
import { MixedWidget6Component } from '../../widgets/mixed/mixed-widget6/mixed-widget6.component';
import { MixedWidget14Component } from '../../widgets/mixed/mixed-widget14/mixed-widget14.component';
import { AdvanceTablesWidget1Component } from '../../widgets/advance-tables/advance-tables-widget1/advance-tables-widget1.component';
import { AdvanceTablesWidget2Component } from '../../widgets/advance-tables/advance-tables-widget2/advance-tables-widget2.component';
import { AdvanceTablesWidget4Component } from '../../widgets/advance-tables/advance-tables-widget4/advance-tables-widget4.component';

@Component({
  selector: 'app-dashboard2',
  templateUrl: './dashboard2.component.html',
  imports:[MixedWidget10Component, MixedWidget11Component, MixedWidget12Component, ListsWidget10Component,
    ListsWidget11Component, ListsWidget1Component, ListsWidget3Component, ListsWidget4Component,
    ListsWidget8Component, ListsWidget9Component, TilesWidget1Component, TilesWidget3Component, TilesWidget10Component
    ,TilesWidget11Component, TilesWidget12Component, TilesWidget13Component, TilesWidget14Component,StatsWidget11Component,
    StatsWidget12Component, MixedWidget1Component, MixedWidget6Component, MixedWidget14Component, AdvanceTablesWidget1Component,
    AdvanceTablesWidget2Component, AdvanceTablesWidget4Component
  ]
})
export class Dashboard2Component implements OnInit {
  constructor() { }

  ngOnInit(): void { }
}
