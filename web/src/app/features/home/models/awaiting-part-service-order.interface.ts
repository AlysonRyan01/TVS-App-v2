export interface AwaitingPartServiceOrder {
    number: number;
    customer: string;
    model: string;
    solution: string | null;
    partPurchasedAt: Date | null;
}