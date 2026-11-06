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
    document: string | null;
}

export type CustomerForm = Omit<Customer, 'id' | 'email'> & { email: string };
