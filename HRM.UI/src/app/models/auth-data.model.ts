import {UserDTO} from '../components/HRM/Management/AccountManagement/Model/account-management.model';

export interface AuthData {
  user: UserDTO;
  token: string;
}