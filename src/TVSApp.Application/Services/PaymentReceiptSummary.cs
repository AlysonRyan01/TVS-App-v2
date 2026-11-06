using TVS_App.Domain.Entities;

namespace TVS_App.Application.Services;

public record PaymentReceiptSummary(decimal ServiceOrderTotal, decimal PaidUntilThisPayment, decimal Balance)
{
    public static PaymentReceiptSummary For(ServiceOrder serviceOrder, Payment payment)
    {
        var paid = serviceOrder.Payments
            .Where(p => p.PaidAt < payment.PaidAt || (p.PaidAt == payment.PaidAt && p.Id <= payment.Id))
            .Sum(p => p.Amount);

        return new PaymentReceiptSummary(serviceOrder.TotalAmount, paid, serviceOrder.TotalAmount - paid);
    }
}
