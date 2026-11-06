import { EEnterprise, EProduct, EServiceOrderStatus } from "../../../shared/enums";

export interface ServiceOrderSearchItem {
    number: number;
    customer: string;
    productType: EProduct;
    brand: string;
    model: string;
    serialNumber: string;
    enterprise: EEnterprise;
    serviceOrderStatus: EServiceOrderStatus;
    isSale: boolean;
    enteredAt: string;
}

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
