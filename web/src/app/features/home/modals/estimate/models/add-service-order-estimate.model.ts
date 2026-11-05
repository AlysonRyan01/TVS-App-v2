import { ERepairResult } from "../../../../../shared/enums";

export interface AddServiceOrderEstimate {
    serviceOrderId: number;
    warranty: string;
    solution: string;
    totalCost: number;
    repairResult: ERepairResult;
    estimateMessage: string;
    partPurchaseCost: number;
}