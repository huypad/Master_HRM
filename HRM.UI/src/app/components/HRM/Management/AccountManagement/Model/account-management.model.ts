export interface LoginModel {
  username: string;
  password: string;
}

export interface UserDTO {
  username: string;
  firstname: string;
  lastname: string;
  isMasterAccount: boolean;

  id?: number | string;
  userId?: number | string;
  fullName?: string;
  email?: string;
  phone?: string;
  avatar?: string;
  token?: string;
  accessToken?: string;
  refreshToken?: string;
  roles?: string[];
  permissions?: string[];
  expiresIn?: number;
  expiration?: string | Date;
  lastLogin?: string | Date;
  status?: boolean | number | string;

  [key: string]: any;
}

export interface User extends UserDTO {}

export class UserModel implements UserDTO {
  username: string = '';
  firstname: string = '';
  lastname: string = '';
  isMasterAccount: boolean = false;

  id?: number | string;
  userId?: number | string;
  fullName?: string;
  email?: string;
  phone?: string;
  avatar?: string;
  token?: string;
  accessToken?: string;
  refreshToken?: string;
  roles?: string[];
  permissions?: string[];
  expiresIn?: number;
  expiration?: string | Date;
  lastLogin?: string | Date;
  status?: boolean | number | string;

  [key: string]: any;

  constructor(init?: Partial<UserDTO>) {
    Object.assign(this, init);

    if ((!this.firstname || !this.lastname) && this.fullName) {
      const parts = this.fullName.trim().split(/\s+/);
      this.firstname = this.firstname || parts[0] || '';
      this.lastname = this.lastname || parts.slice(1).join(' ');
    }
  }
}