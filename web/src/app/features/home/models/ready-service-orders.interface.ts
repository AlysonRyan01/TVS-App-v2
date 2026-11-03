import { ERepairResult, ERepairStatus } from '../../../shared/enums';

export interface ReadyServiceOrder {
    number: number;
    customer: string;
    model: string;
    repairDate: Date;
    situation: keyof typeof ERepairResult;
    response: keyof typeof ERepairStatus;
}