import { Component, OnInit } from '@angular/core';
import { RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';
import { LayoutService } from '../../../../_metronic/core';

@Component({
  selector: 'app-aside-dynamic',
  standalone: true,
  templateUrl: './aside-dynamic.component.html',
  styleUrls: ['./aside-dynamic.component.scss'],
  imports: [CommonModule, RouterModule],
})
export class AsideDynamicComponent implements OnInit {
  // Layout properties
  disableAsideSelfDisplay!: boolean;
  headerLogo!: string;
  brandSkin!: string;
  ulCSSClasses!: string;
  asideMenuCSSClasses!: string;
  asideMenuDropdown!: string;
  brandClasses!: string;
  asideMenuScroll = 1;
  asideSelfMinimizeToggle = false;

  constructor(private layout: LayoutService) {}

  ngOnInit(): void {
    this.disableAsideSelfDisplay =
      this.layout.getProp('aside.self.display') === false;

    this.brandSkin = this.layout.getProp('brand.self.theme') || 'light';

    this.headerLogo = './assets/media/logos/logo.png';

    this.ulCSSClasses = this.layout.getProp('aside_menu_nav') || '';

    this.asideMenuCSSClasses =
      this.layout.getStringCSSClasses('aside_menu') || '';

    this.asideMenuDropdown = this.layout.getProp('aside.menu.dropdown')
      ? '1'
      : '0';

    this.brandClasses = this.layout.getProp('brand') || '';

    this.asideSelfMinimizeToggle =
      this.layout.getProp('aside.self.minimize.toggle') || false;

    this.asideMenuScroll = this.layout.getProp('aside.menu.scroll') ? 1 : 0;
  }
}
