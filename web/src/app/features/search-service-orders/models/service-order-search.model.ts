import { EEnterprise, EProduct, EServiceOrderStatus } from "../../../shared/enums";

/** Linha da tabela de GET /service-orders/search. */
export interface ServiceOrderSearchItem {
    number: number;
    customer: string;
    productType: EProduct;
    brand: string;
    model: string;
    serialNumber: string;
    enterprise: EEnterprise;
    serviceOrderStatus: EServiceOrderStatus;
    /** OS de venda (defeito "VENDA"): a tela mostra "Venda" no lugar da situação. */
    isSale: boolean;
    enteredAt: string;
}

/** Um filtro por vez; o período (from/to, yyyy-MM-dd) conta como um. */
export interface ServiceOrderSearchParams {
    number?: number | null;
    customer?: string | null;
    model?: string | null;
    serialNumber?: string | null;
    from?: string | null;
    to?: string | null;
    pageNumber: number;
    pageSize: number;
}

/** Corpo de PUT /service-orders/{id} (Alterar OS). Datas: ISO; enums numéricos. */
export interface EditServiceOrder {
    brand: string;
    model: string;
    serialNumber: string;
    defect: string;
    accessories: string;
    type: EProduct;
    location: string;
    enterprise: EEnterprise;
    entryDate: string | null;
    inspectionDate: string | null;
    responseDate: string | null;
    repairDate: string | null;
    purchasePartDate: string | null;
    deliveryDate: string | null;
    solution: string;
    guarantee: string;
    partCost: number;
    laborCost: number;
}
