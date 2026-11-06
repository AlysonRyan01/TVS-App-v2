/** Filtros da grade de clientes (GET /customers/search). Filtros vazios são omitidos. */
export interface CustomerSearchParams {
    name?: string;
    phone?: string;
    document?: string;
    pageNumber: number;
    pageSize: number;
}
