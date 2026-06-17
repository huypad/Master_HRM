import { ChangeDetectionStrategy, ChangeDetectorRef, Component, EventEmitter, Input, OnDestroy, OnInit, Output } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, FormsModule } from '@angular/forms';
import { BehaviorSubject, Subscription } from 'rxjs';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';

// Material modules
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatFormFieldModule } from '@angular/material/form-field';
import { ClickOutside2Directive } from './click-outside2.directive';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-jee-search-form',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    FormsModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatInputModule,
    MatCheckboxModule,
    MatFormFieldModule, CommonModule, ReactiveFormsModule,
  ],
  templateUrl: './jee-search-form.component.html',
  styleUrls: ['./jee-search-form.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class JeeSearchFormComponent implements OnInit, OnDestroy {
  private _errorMessage$ = new BehaviorSubject<string>('');
  private subscriptions: Subscription[] = [];
  searchGroup!: FormGroup;
  filterGroup!: FormGroup;
  titlekeyword: string = '';
  @Input() showSearch: any = {};
  @Output() keywordEvent: EventEmitter<string> = new EventEmitter<string>();
  @Output() filterEvent: EventEmitter<any> = new EventEmitter<any>();
  isAdmin: boolean = false;
  daKhoa: boolean = false;
  showFilter: boolean = false;

  clickSelection: boolean = false;

  get errorMessage$() {
    return this._errorMessage$.asObservable();
  }

  constructor(
    private fb: FormBuilder,
    public cd: ChangeDetectorRef,
  ) { }

  ngOnInit() {
    this.titlekeyword = this.showSearch?.titlekeyword ?? 'Nhập từ khoá';
    this.searchForm();
    this.filterForm();
  }

  ngOnDestroy(): void {
    this.clearFilter();
    this.subscriptions.forEach((sb) => sb.unsubscribe());
  }

  searchForm() {
    this.searchGroup = this.fb.group({
      keyword: [''],
    });
    const searchEvent = this.searchGroup.controls['keyword'].valueChanges
      .pipe(
        debounceTime(300),
        distinctUntilChanged()
      )
      .subscribe((val) => {
        this.search(val);
      });
    this.subscriptions.push(searchEvent);
  }

  search(val: any) {
    this.keywordEvent.emit(val);
  }

  filterForm() {
    this.filterGroup = this.fb.group({
      keyword: [''],
      username: [''],
      tennhanvien: [''],
    });
  }

  clickSearch() {
    const filter: { [key: string]: any } = {};
    if (this.filterGroup.controls['keyword'].value) filter['keyword'] = this.filterGroup.controls['keyword'].value;
    if (this.filterGroup.controls['username'].value) filter['username'] = this.filterGroup.controls['username'].value;
    if (this.filterGroup.controls['tennhanvien'].value) filter['tennhanvien'] = this.filterGroup.controls['tennhanvien'].value;
    if (this.isAdmin) filter['isadmin'] = this.isAdmin;
    if (this.daKhoa) filter['dakhoa'] = this.daKhoa;
    this.filterEvent.emit(filter);
  }

  clickShowFilter(val: boolean) {
    this.showFilter = !val;
  }

  clearFilter() {
    this.searchGroup.reset();
    this.filterGroup.reset();
    this.daKhoa = false;
    this.isAdmin = false;
    this.showFilter = false;
    this.clickSearch();
  }

  clickOutSideFilter() {
    if (!this.clickSelection) this.showFilter = false;
    setTimeout(() => {
      this.clickSelection = false;
    }, 3000);
  }
  clickSelect() {
    this.clickSelection = true;
  }
}
