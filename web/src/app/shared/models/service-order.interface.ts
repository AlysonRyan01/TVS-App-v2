import { EEnterprise, EProduct, ERepairResult, ERepairStatus, EServiceOrderStatus } from "../enums";

export interface ServiceOrder {
    id: number;
 
    customerId: number;
    name: string;
    street: string;
    neighborhood: string;
    city: string;
    number: string;
    zipCode: string;
    state: string;
    phone: string;
    phone2: string;
    email?: string | null;
    document: string | null;
 
    brand: string;
    model: string;
    serialNumber: string;
    defect?: string | null;
    accessories?: string | null;
    type: EProduct;
    location: string;
 
    enterprise: EEnterprise;
    entryDate: string;
    inspectionDate?: string | null;
    responseDate?: string | null;
    repairDate?: string | null;
    purchasePartDate?: string | null;
    deliveryDate?: string | null;
 
    solution?: string | null;
    estimateMessage?: string | null;
    guarantee?: string | null;
    partCost: number;
    laborCost: number;
    partPurchaseCost: number;
    totalAmount: number;
 
    serviceOrderStatus: EServiceOrderStatus;
    repairStatus: ERepairStatus;
    repairResult?: ERepairResult | null;
 
    formattedAddress: string;
}