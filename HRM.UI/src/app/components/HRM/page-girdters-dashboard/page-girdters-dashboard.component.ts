import { Component, ChangeDetectionStrategy, ChangeDetectorRef, EventEmitter, OnInit, Type } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NgbDropdownModule } from '@ng-bootstrap/ng-bootstrap';
import { GridsterModule, GridsterItem, GridsterConfig, DisplayGrid } from 'angular-gridster2';
import { DynamicModule } from 'ng-dynamic-component';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { PerfectScrollbarModule } from 'ngx-perfect-scrollbar';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { RouterModule } from '@angular/router';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';

// Các service & model
import { PageGirdtersDashboardService } from './Services/page-girdters-dashboard.service';
import { AddCloseWidgetDialogComponent } from './widgets/add-close-widget-dialog/add-close-widget-dialog.component';
import { DanhSachTruyCapNhanhWidgetComponent } from './widgets/danh-sach-truy-cap-nhanh/danh-sach-truy-cap-nhanh.component';
import { Widget, WidgetModel, Dashboard } from './Model/page-girdters-dashboard.model';
import { ResultModel } from '../_core/models/_base.model';

@Component({
  selector: 'm-page-girdters-dashboard',
  standalone: true,
  templateUrl: './page-girdters-dashboard.component.html',
  styleUrls: ['./page-girdters-dashboard.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule,
    NgbDropdownModule,
    GridsterModule,
    DynamicModule,
    MatIconModule,
    MatButtonModule,
    MatButtonToggleModule,
    MatSlideToggleModule,
    MatTableModule,
    RouterModule,
    MatDialogModule,
  ],
  providers: [PageGirdtersDashboardService],
})
export class PageGidtersDashboardComponent implements OnInit {
  public listWidget: Widget[] = [];
  public options!: GridsterConfig;
  public WidgetDashboard!: Dashboard;
  public dashboard: Widget[] = [];
  private resizeEvent: EventEmitter<any> = new EventEmitter<any>();
  private configureEvent: EventEmitter<boolean> = new EventEmitter<boolean>();
  public showConfig: boolean = false;

  public inputs = {
    widget: '',
    resizeEvent: this.resizeEvent,
    configureEvent: this.configureEvent,
  };

  public outputs = {
    onSomething: (type: any) => console.log(type),
  };

  showSetting: boolean[] = []; // Mảng để lưu trạng thái show/hide setting

  constructor(
    private changeDetect: ChangeDetectorRef,
    public dialog: MatDialog,
    private pageGirdtersDashboardService: PageGirdtersDashboardService
  ) {}

  ngOnInit() {
    this.listWidget = this.pageGirdtersDashboardService.getWidgets();
    this.WidgetDashboard = new Dashboard();
    this.options = this.pageGirdtersDashboardService.getDashBoardOptions();
    this.options.displayGrid = DisplayGrid.OnDragAndResize;

    this.options.itemChangeCallback = (item: GridsterItem, itemComponent) => {
      const widget = item as Widget;
      const widgetModel = new WidgetModel(widget);
      this.pageGirdtersDashboardService.postUpdateWidget(widgetModel).subscribe((res) => {
        if (res && res.status === 1) {
          this.resizeEvent.emit(widget);
          this.changeDetect.detectChanges();
        }
      });
    };

    this.options.itemResizeCallback = (item: GridsterItem, itemComponent) => {
      const widget = item as Widget;
      const widgetModel = new WidgetModel(widget);
      this.pageGirdtersDashboardService.postUpdateWidget(widgetModel).subscribe((res) => {
        if (res && res.status === 1) {
          this.resizeEvent.emit(widget);
          this.changeDetect.detectChanges();
        }
      });
    };

    this.pageGirdtersDashboardService.getDSWidget().subscribe((res: ResultModel<Widget>) => {
      if (res.data && res.data.length > 0) {
        this.WidgetDashboard.widgets = res.data;
        this.WidgetDashboard.widgets.forEach((widget: Widget) => {
          const matchedWidget = this.listWidget.find(w => w.componentName === widget.componentName);
          if (matchedWidget) {
            widget.componentType = matchedWidget.componentType;
          }
        });
        this.dashboard = this.WidgetDashboard.widgets;
        // Khởi tạo showSetting với độ dài bằng dashboard
        this.showSetting = new Array(this.dashboard.length).fill(false);
      } else {
        this.dashboard = [];
        this.showSetting = [];
      }
      this.changeDetect.detectChanges();
    });
  }

  changedOptions() {
    if (this.options.api && this.options.api.optionsChanged) {
      this.options.api.optionsChanged();
    }
  }

  getInput(widget: Widget): any {
    return {
      widget: widget,
      resizeEvent: this.resizeEvent,
      configureEvent: this.configureEvent,
    };
  }

  getShowSetting(index: number, mouseover: boolean) {
    if (mouseover) {
      this.showSetting = new Array(this.dashboard.length).fill(false);
      this.showSetting[index] = true;
    } else {
      this.showSetting = new Array(this.dashboard.length).fill(false);
    }
    this.changeDetect.detectChanges();
  }

  closeWidget(id: number) { // Đổi id từ string thành number
    this.pageGirdtersDashboardService.deleteWidget(id).subscribe((res) => {
      if (res && res.status === 1) {
        this.ngOnInit();
        this.changeDetect.detectChanges();
      }
    });
  }

  AddCloseWidget() {
    const dialogRef = this.dialog.open(AddCloseWidgetDialogComponent, {
      data: {},
      position: { top: '60px' },
      panelClass: 'no-padding',
    });
    dialogRef.afterClosed().subscribe((res) => {
      if (res) {
        this.ngOnInit();
        this.changeDetect.detectChanges();
      }
    });
  }
}