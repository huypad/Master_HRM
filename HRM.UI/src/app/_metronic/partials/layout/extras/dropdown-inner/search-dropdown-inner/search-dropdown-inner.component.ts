import {
  Component,
  OnInit,
  ViewChild,
  ElementRef,
  ChangeDetectorRef,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { SearchResultComponent } from '../search-dropdown-inner/search-result/search-result.component';

export interface SearchItem {
  title: string;
  type: number;
  items: {
    title: string;
    description: string;
    svgPath?: string;
    imgPath?: string;
    iconClasses?: string;
  }[];
}

const documents: SearchItem = {
  title: 'Documents',
  type: 0,
  items: [
    {
      svgPath: 'assets/media/svg/files/doc.svg',
      title: 'AirPlus Requirements',
      description: 'by Grog John',
    },
    {
      svgPath: 'assets/media/svg/files/pdf.svg',
      title: 'TechNav Documentation',
      description: 'by Mary Brown',
    },
    {
      svgPath: 'assets/media/svg/files/xml.svg',
      title: 'All Framework Docs',
      description: 'by Nick Stone',
    },
    {
      svgPath: 'assets/media/svg/files/csv.svg',
      title: 'Finance & Accounting Reports',
      description: 'by Jhon Larson',
    },
  ],
};

const members: SearchItem = {
  title: 'Members',
  type: 1,
  items: [
    {
      imgPath: 'assets/media/users/300_20.jpg',
      title: 'Milena Gibson',
      description: 'UI Designer',
    },
    {
      imgPath: 'assets/media/users/300_15.jpg',
      title: 'Stefan JohnStefan',
      description: 'Marketing Manager',
    },
    {
      imgPath: 'assets/media/users/300_12.jpg',
      title: 'Anna Strong',
      description: 'Software Developer',
    },
    {
      imgPath: 'assets/media/users/300_16.jpg',
      title: 'Nick Bold',
      description: 'Project Coordinator',
    },
  ],
};

const files: SearchItem = {
  title: 'Files',
  type: 2,
  items: [
    {
      iconClasses: 'flaticon-psd text-primary',
      title: '79 PSD files generated',
      description: 'by Grog John',
    },
    {
      iconClasses: 'flaticon2-supermarket text-warning',
      title: '$2900 worth products sold',
      description: 'Total 234 items',
    },
    {
      iconClasses: 'flaticon-safe-shield-protection text-info',
      title: '4 New items submitted',
      description: 'Marketing Manager',
    },
    {
      iconClasses: 'flaticon-safe-shield-protection text-warning',
      title: '4 New items submitted',
      description: 'Marketing Manager',
    },
  ],
};

@Component({
  selector: 'app-search-dropdown-inner',
  templateUrl: './search-dropdown-inner.component.html',
  imports: [CommonModule, SearchResultComponent],
})
export class SearchDropdownInnerComponent implements OnInit {
  @ViewChild('searchInput', { static: true }) searchInput!: ElementRef;
  data: SearchItem[] | null = null;
  loading: boolean = false;

  constructor(private cdr: ChangeDetectorRef) {}

  ngOnInit(): void {}

  search(e: Event) {
    const target = e.target as HTMLInputElement;
    this.data = null;
    if (target.value.length > 1) {
      this.loading = true;
      setTimeout(() => {
        this.data = [documents, members, files];
        this.loading = false;
        this.cdr.markForCheck();
      }, 500);
    }
  }

  clear(e: Event) {
    this.data = null;
    this.searchInput.nativeElement.value = '';
  }

  openChange() {
    setTimeout(() => this.searchInput.nativeElement.focus());
  }

  showCloseButton() {
    return this.data && this.data.length && !this.loading;
  }
}