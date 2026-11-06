/** Endereço obtido pelo CEP (GET /addresses/{cep}). */
export interface Address {
    zipCode: string;
    street: string;
    neighborhood: string;
    city: string;
    state: string;
}
