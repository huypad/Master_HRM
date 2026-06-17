import { Component, OnInit, AfterViewInit } from '@angular/core';
import { Observable } from 'rxjs';
import { SubheaderService } from '../_services/subheader.service';
import { CommonModule } from '@angular/common';
declare var KTUtil: any;
declare var KTLayoutSubheader: any;
import { Router, ResolveEnd } from '@angular/router';
import { filter } from 'rxjs/operators';
import {Subheader1Component} from '../subheader1/subheader1.component';
import {Subheader2Component} from '../subheader2/subheader2.component';
import {Subheader3Component} from '../subheader3/subheader3.component';
import {Subheader4Component} from '../subheader4/subheader4.component';
import {Subheader5Component} from '../subheader5/subheader5.component';
import {Subheader6Component} from '../subheader6/subheader6.component';
import {Subheader7Component} from '../subheader7/subheader7.component';
import { AsyncPipe } from '@angular/common';

@Component({
  selector: 'app-subheader-wrapper',
  templateUrl: './subheader-wrapper.component.html',
imports: [
    Subheader1Component,Subheader2Component,Subheader3Component,Subheader4Component,
    Subheader5Component,Subheader6Component, Subheader7Component, AsyncPipe, CommonModule
  ],
})
export class SubheaderWrapperComponent implements OnInit, AfterViewInit {
  subheaderVersion$: Observable<string>;
  constructor(private subheader: SubheaderService, private router: Router) {
    this.subheader.setDefaultSubheader();
    this.subheaderVersion$ = this.subheader.subheaderVersionSubject.asObservable();

    const initSubheader = () => {
      setTimeout(() => {
        this.subheader.updateAfterRouteChanges(this.router.url);
      }, 0);
    };

    initSubheader();
    // subscribe to router events
    this.router.events
      .pipe(filter((event) => event instanceof ResolveEnd))
      .subscribe(initSubheader);
  }

  ngOnInit(): void {}

  ngAfterViewInit() {
    KTUtil.ready(() => {
      KTLayoutSubheader.init('kt_subheader');
    });
  }
}
