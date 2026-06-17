import { Routes } from '@angular/router';
import { NoticeComponent } from './notice/notice.component';
import { CodePreviewComponent } from './code-preview/code-preview.component';

export const generalRoutes: Routes = [
  {
    path: 'notice',
    component: NoticeComponent,
  },
  {
    path: 'code-preview',
    component: CodePreviewComponent,
  },
];