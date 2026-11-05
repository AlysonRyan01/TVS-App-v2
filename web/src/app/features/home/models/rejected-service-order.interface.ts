export interface RejectedServiceOrder {
    number: number;
    customer: string;
    model: string;
    solution: string | null;
    rejectedAt: Date | null;
}