import { ERepairResult } from "../../../../../shared/enums";

export interface AddServiceOrderQuote {
    serviceOrderId: number;
    guarantee: string;
    solution: string;
    totalCost: number;
    repairResult: ERepairResult;
    estimateMessage: string;
    partPurchaseCost: number;
}