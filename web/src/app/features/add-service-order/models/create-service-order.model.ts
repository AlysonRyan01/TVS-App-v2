import { EEnterprise, EProduct } from "../../../shared/enums";

/** Corpo de POST /service-orders. */
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
