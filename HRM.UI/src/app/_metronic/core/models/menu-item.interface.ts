export interface MenuItem {
  title: string;
  page: string;
  icon?: string;
  submenu?: MenuItem[];
  isOpen?: boolean;
}

export interface MenuConfig {
  items: MenuItem[];
}