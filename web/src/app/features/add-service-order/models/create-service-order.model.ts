import { EEnterprise, EProduct } from "../../../shared/enums";

export interface CreateServiceOrder {
    customerId: number;
    productBrand: string;
    productModel: string;
    productSerialNumber: string;
    productDefect: string;
    accessories: string;
    productType: EProduct;
    enterprise: EEnterprise;
}
