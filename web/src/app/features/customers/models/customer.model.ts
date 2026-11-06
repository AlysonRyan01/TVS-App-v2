/** Cliente como a API devolve (CustomerDto). */
export interface Customer {
    id: number;
    name: string;
    street: string;
    neighborhood: string;
    city: string;
    number: string;
    zipCode: string;
    state: string;
    phone: string;
    phone2: string;
    email: string | null;
    /** CPF ou CNPJ sem pontuação; null quando não informado. */
    document: string | null;
}

/** Corpo de POST /customers e PUT /customers/{id}. */
export type CustomerForm = Omit<Customer, 'id' | 'email'> & { email: string };
