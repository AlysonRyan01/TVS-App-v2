export interface AwaitingResponseServiceOrder {
    number: number;
    customer: string;
    model: string;
    solution: string | null;
    amount: number;
    evaluatedAt: Date | null;
}