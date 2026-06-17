// Models
export type { BaseModel } from './models/base.model'; // Sử dụng export type cho interface
export * from './models/table.model';
export * from './models/sort.model';
export * from './models/paginator.model';
export * from './models/grouping.model';
export * from './models/search.model';
export * from './models/filter.model';

// Directives
// Services
export * from './services/table.service';
export * from './services/table.extended.service';

// Module
export { crudTableRoutes } from './crud-table.routes'; // Sửa CRUDTableModule thành crudTableRoutes