import { Component, OnInit } from '@angular/core';
import { LayoutService } from '../../../../../core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-cart-dropdown-inner',
  templateUrl: './cart-dropdown-inner.component.html',
  styleUrls: ['./cart-dropdown-inner.component.scss'],
  imports: [CommonModule]
})
export class CartDropdownInnerComponent implements OnInit {
  extrasCartDropdownStyle: 'light' | 'dark' = 'light';
  constructor(private layout: LayoutService) {}

  ngOnInit(): void {
    this.extrasCartDropdownStyle = this.layout.getProp(
      'extras.cart.dropdown.style'
    );
  }
}
