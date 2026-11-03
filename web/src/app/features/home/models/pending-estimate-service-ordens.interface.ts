import { EEnterprise } from '../../../shared/enums';

export interface PendingEstimateServiceOrder {
    number: number;
    customer: string;
    company: keyof typeof EEnterprise;
    model: string;
    defect: string;
    entryDate: Date;
}