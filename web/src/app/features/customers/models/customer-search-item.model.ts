/** Item da busca rápida de clientes (GET /customers?name=): só o necessário para a lista. */
export interface CustomerSearchItem {
    id: number;
    name: string;
    phone: string;
}
