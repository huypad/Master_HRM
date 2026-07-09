export interface UserDTO {
  id?: number;
  userId?: number;
  username: string;
  userName?: string;
  firstname: string;
  lastname: string;
  fullName?: string;
  email?: string;
  phoneNumber?: string;
  avatar?: string;
  role?: string;
  token?: string;
  isMasterAccount: boolean;
}

export interface LoginModel {
  username?: string;
  userName?: string;
  email?: string;
  password?: string;
  rememberMe?: boolean;
}

export interface User {
  id?: number;
  username: string;
  userName?: string;
  firstname: string;
  lastname: string;
  fullName?: string;
  email?: string;
  avatar?: string;
  role?: string;
  isMasterAccount: boolean;
}

export class UserModel {
  id?: number;
  username = '';
  userName?: string;
  firstname = '';
  lastname = '';
  fullName?: string;
  email?: string;
  avatar?: string;
  role?: string;
  token?: string;
  isMasterAccount = false;

  user: User = {
    username: '',
    firstname: '',
    lastname: '',
    isMasterAccount: false,
  };

  constructor(init?: Partial<UserModel>) {
    Object.assign(this, init);

    this.username = this.username || this.userName || this.user?.username || '';
    this.firstname = this.firstname || this.user?.firstname || '';
    this.lastname = this.lastname || this.user?.lastname || '';
    this.isMasterAccount = this.isMasterAccount ?? this.user?.isMasterAccount ?? false;

    this.user = {
      username: this.user?.username || this.username || '',
      userName: this.user?.userName || this.userName,
      firstname: this.user?.firstname || this.firstname || '',
      lastname: this.user?.lastname || this.lastname || '',
      fullName: this.user?.fullName || this.fullName,
      email: this.user?.email || this.email,
      avatar: this.user?.avatar || this.avatar,
      role: this.user?.role || this.role,
      isMasterAccount: this.user?.isMasterAccount ?? this.isMasterAccount ?? false,
    };
  }
}
