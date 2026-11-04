namespace TVS_App.Domain.Entities;

public class Payment : Entity
{
    public decimal Amount { get; private set; }
    public DateTime PaidAt { get; private set; }

    public long ServiceOrderId { get; private set; }
    public ServiceOrder ServiceOrder { get; private set; } = null!;
    
    protected Payment() { }

    public Payment(decimal amount, DateTime paidAt, long serviceOrderId)
    {
        Amount = amount;
        PaidAt = paidAt;
        ServiceOrderId = serviceOrderId;
    }

    public void Update(decimal amount, DateTime paidAt)
    {
        Amount = amount;
        PaidAt = paidAt;
    }
}