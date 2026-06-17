import { Component, OnInit } from '@angular/core';
import { Observable } from 'rxjs';
import { LayoutService } from '../../../../../core';
// Import đúng UserDTO theo models của bạn
import { UserDTO } from '../../../../../../components/HRM/Management/AccountManagement/Model/account-management.model';
import { AuthService } from '../../../../../../services/auth.service';
import { FirstLetterPipe } from '../../../../../core/pipes/first-letter.pipe';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-user-dropdown-inner',
  templateUrl: './user-dropdown-inner.component.html',
  styleUrls: ['./user-dropdown-inner.component.scss'],
  imports: [FirstLetterPipe, CommonModule]
})
export class UserDropdownInnerComponent implements OnInit {
  extrasUserDropdownStyle: 'light' | 'dark' = 'light';
  user$: Observable<UserDTO | null>;

  constructor(private layout: LayoutService, private auth: AuthService) {
    // Sử dụng đúng observable đã expose trong service
    this.user$ = this.auth.currentUser$;
  }

  ngOnInit(): void {
    this.extrasUserDropdownStyle = this.layout.getProp(
      'extras.user.dropdown.style'
    );
  }

  logout() {
    this.auth.logout();
    document.location.reload();
  }
}
