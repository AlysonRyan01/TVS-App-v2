import { ERepairResult, ERepairStatus } from '../../../shared/enums';

export interface ReadyForPickupServiceOrder {
    number: number;
    customer: string;
    model: string;
    repairedAt: Date | null;
    repairResult: keyof typeof ERepairResult | null;
    repairStatus: keyof typeof ERepairStatus | null;
}