import { Component, OnInit, Input } from '@angular/core';
import { CommonModule } from '@angular/common'

@Component({
  selector: 'app-notice',
  templateUrl: './notice.component.html',
  imports: [
    CommonModule, 
  ]
})
export class NoticeComponent implements OnInit {
  // Public properties
  @Input() classes!: string;
  @Input() icon!: string;
  @Input() svg!: string;

  constructor() {}

  ngOnInit(): void {}
}
