/** GET /metrics (DashboardMetricsDto). */
export interface DashboardMetrics {
    totalOrders: number;
    activeOrders: number;
    totalRevenue: number;
    approvalRate: number;
    averageCycleDays: number;
    totalSales: number;
    totalSalesRevenue: number;
    pendingDeliveryRevenue: number;
    totalPartsSpend: number;
    totalValue: number;
    netTotal: number;
    deliveredProducts: DeliveredProduct[];
    ordersByStatusLabels: string[];
    ordersByStatusValues: number[];
    repairResultLabels: string[];
    repairResultValues: number[];
}

export interface DeliveredProduct {
    id: number;
    customerName: string;
    brand: string;
    model: string;
    productLabel: string;
    typeLabel: string;
    /** "Reparo" ou "Venda". */
    statusLabel: string;
    enterpriseLabel: string;
    deliveryDate: string;
    totalAmount: number;
    partPurchaseCost: number;
}

/** Mês/Ano ou De/Até (yyyy-MM-dd). */
export interface MetricsPeriod {
    month?: number;
    year?: number;
    from?: string;
    to?: string;
}
