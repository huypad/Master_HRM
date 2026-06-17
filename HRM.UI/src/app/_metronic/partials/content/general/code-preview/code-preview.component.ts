import {
  Component,
  OnInit,
  Input,
  ElementRef,
  AfterViewInit,
} from '@angular/core';
declare var KTLayoutExamples: any;
import { HighlightModule, HIGHLIGHT_OPTIONS } from 'ngx-highlightjs';
import { NgbNavModule, NgbTooltipModule } from '@ng-bootstrap/ng-bootstrap';
import { SafePipe } from '../../../../core/pipes/safe.pipe';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-code-preview',
  templateUrl: './code-preview.component.html',
  styleUrls: ['./code-preview.component.scss'],
  imports: [HighlightModule, NgbNavModule, NgbTooltipModule, SafePipe, CommonModule]
})
export class CodePreviewComponent implements OnInit, AfterViewInit {
  // Public properties
  @Input() viewItem: any;
  constructor(private el: ElementRef) {}

  ngOnInit(): void {}

  ngAfterViewInit() {
    const elements = this.el.nativeElement.querySelectorAll(
      '.example.example-compact'
    );
    KTLayoutExamples.init(elements);
  }
}
