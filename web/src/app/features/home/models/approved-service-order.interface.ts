import { EEnterprise } from '../../../shared/enums';

export interface ApprovedServiceOrder {
    number: number;
    company: keyof typeof EEnterprise;
    customer: string;
    model: string;
    solution: string | null;
    approvedAt: Date | null;
}