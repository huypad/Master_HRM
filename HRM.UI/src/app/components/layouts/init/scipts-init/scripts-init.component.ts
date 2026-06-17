import { Component, OnInit, AfterViewInit } from '@angular/core';
declare var KTUtil: any;
declare var KTLayoutAsideToggle: any;
declare var KTLayoutStickyCard: any;
declare var KTLayoutStretchedCard: any;
import { LayoutService } from '../../../../_metronic/core';
declare var KTLayoutBrand: any;
declare var KTLayoutAside : any;
declare var KTLayoutAsideMenu: any;

@Component({
  selector: 'app-scripts-init',
  templateUrl: './scripts-init.component.html',
})
export class ScriptsInitComponent implements OnInit, AfterViewInit {
  asideSelfMinimizeToggle = false;

  constructor(private layout: LayoutService) {}

  ngOnInit(): void {
    this.asideSelfMinimizeToggle = this.layout.getProp('aside.self.minimize.toggle');
  }

  ngAfterViewInit() {
    KTUtil.ready(() => {
      // Init Brand Panel For Logo
      KTLayoutBrand.init('kt_brand');
      // Init Aside
      KTLayoutAside.init('kt_aside');
      // Init Aside Menu
      KTLayoutAsideMenu.init('kt_aside_menu');

      if (this.asideSelfMinimizeToggle) {
        // Init Aside Menu Toggle
        KTLayoutAsideToggle.init('kt_aside_toggle');
      }

      // Init Sticky Card
      KTLayoutStickyCard.init('kt_page_sticky_card');
      // Init Stretched Card
      KTLayoutStretchedCard.init('kt_page_stretched_card');
    });
  }
}
