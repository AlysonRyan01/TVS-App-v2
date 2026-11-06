namespace TVS_App.Application.Observability;

public static class BusinessEvents
{
    public const string MeterName = "TVS";

    public const string AuthLogin = "auth.login";
    public const string CustomerCreated = "customer.created";
    public const string CustomerUpdated = "customer.updated";
    public const string ServiceOrderCreated = "service_order.created";
    public const string ServiceOrderUpdated = "service_order.updated";
    public const string SaleCreated = "sale.created";
    public const string EstimateAdded = "estimate.added";
    public const string EstimateApproved = "estimate.approved";
    public const string EstimateRejected = "estimate.rejected";
    public const string EstimateApprovedByCustomer = "estimate.approved_by_customer";
    public const string EstimateRejectedByCustomer = "estimate.rejected_by_customer";
    public const string PartPurchased = "part.purchased";
    public const string PartPurchaseCostUpdated = "part.purchase_cost_updated";
    public const string ServiceOrderReadyForPickup = "service_order.ready_for_pickup";
    public const string ServiceOrderRepaired = "service_order.repaired";
    public const string ServiceOrderDelivered = "service_order.delivered";
    public const string ServiceOrderLocationUpdated = "service_order.location_updated";
    public const string ServiceOrderPdfReprinted = "service_order.pdf_reprinted";
    public const string PaymentAdded = "payment.added";
    public const string PaymentUpdated = "payment.updated";
    public const string PaymentRemoved = "payment.removed";
    public const string PaymentReceiptPrinted = "payment.receipt_printed";
    public const string WhatsAppEnqueued = "whatsapp.enqueued";
    public const string WhatsAppSent = "whatsapp.sent";
    public const string WhatsAppFailed = "whatsapp.failed";
}
