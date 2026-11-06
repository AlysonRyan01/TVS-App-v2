import { EProduct } from "../../../shared/enums";

export interface CreateSale {
    customerId: number;
    productType: EProduct;
    productBrand: string;
    productModel: string;
    productSerialNumber: string;
    guarantee: string;
    amount: number;
}
