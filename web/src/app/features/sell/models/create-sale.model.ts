import { EProduct } from "../../../shared/enums";

/** Corpo de POST /service-orders/sales (a venda é uma OS com defeito "VENDA"). */
export interface CreateSale {
    customerId: number;
    productType: EProduct;
    productBrand: string;
    productModel: string;
    productSerialNumber: string;
    guarantee: string;
    amount: number;
}
