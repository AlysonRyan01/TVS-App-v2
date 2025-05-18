using System.Text.Json.Serialization;
using TVS_App.Domain.Enums;
using TVS_App.Domain.Exceptions;
using TVS_App.Domain.ValueObjects.ServiceOrder;

namespace TVS_App.Domain.Entities;

public class ServiceOrder : Entity
{
    protected ServiceOrder() { }

    [JsonConstructor]
    public ServiceOrder(EEnterprise enterprise, long customerId, Product product)
    {
        EntryDate = DateTime.UtcNow;
        Enterprise = enterprise;
        CustomerId = customerId;
        ServiceOrderStatus = EServiceOrderStatus.Entered;
        RepairStatus = ERepairStatus.Entered;
        Product = product;
        SecurityCode = GenerateRandomCode();
    }

    public string SecurityCode { get; private set; } = null!;
    public long CustomerId { get; private set; }
    [JsonPropertyName("customer")]
    [JsonInclude]
    public Customer Customer { get; private set; } = null!;
    [JsonPropertyName("product")]
    [JsonInclude]
    public Product Product { get; private set; } = null!;
    public EEnterprise Enterprise { get; private set; }
    public DateTime EntryDate { get; set; }
    public DateTime? InspectionDate { get; set; }
    public DateTime? ResponseDate { get; set; }
    public DateTime? RepairDate { get; set; }
    public DateTime? PurchasePartDate { get; set; }
    public DateTime? DeliveryDate { get; set; }
    [JsonPropertyName("solution")]
    [JsonInclude]
    public Solution? Solution { get; private set; }
    [JsonInclude]
    public string? EstimateMessage { get; private set; }
    [JsonPropertyName("guarantee")]
    [JsonInclude]
    public Guarantee? Guarantee { get; private set; }
    [JsonPropertyName("partCost")]
    [JsonInclude]
    public PartCost PartCost { get; private set; } = new(0);
    [JsonPropertyName("laborCost")]
    [JsonInclude]
    public LaborCost LaborCost { get; private set; } = new(0);
    [JsonPropertyName("totalAmount")]
    [JsonInclude]
    public decimal TotalAmount => PartCost.ServiceOrderPartCost + LaborCost.ServiceOrderLaborCost;
    [JsonInclude]
    public EServiceOrderStatus ServiceOrderStatus { get; private set; }
    [JsonInclude]
    public ERepairStatus RepairStatus { get; private set; }
    [JsonInclude]
    public ERepairResult? RepairResult { get; set; }

    public void AddEstimate(string solution, string? guarantee, decimal partCost, decimal laborCost, ERepairResult repairResult, string estimateMessage)
    {
        Solution = new Solution(solution);
        Guarantee = new Guarantee(guarantee ?? "");
        PartCost = new PartCost(partCost);
        LaborCost = new LaborCost(laborCost);
        ServiceOrderStatus = EServiceOrderStatus.Evaluated;
        RepairStatus = ERepairStatus.Waiting;
        RepairResult = repairResult;
        InspectionDate = DateTime.UtcNow;
        EstimateMessage = estimateMessage;

        if (RepairResult == ERepairResult.Unrepaired || RepairResult == ERepairResult.NoDefectFound)
            RepairStatus = ERepairStatus.Approved;
    }

    public void ApproveEstimate()
    {
        if (string.IsNullOrEmpty(Solution?.ServiceOrderSolution))
            throw new EntityException<ServiceOrder>("A solução não pode estar nula ao adicionar a aprovação");

        RepairStatus = ERepairStatus.Approved;
        ResponseDate = DateTime.UtcNow;
    }

    public void RejectEstimate()
    {
        if (string.IsNullOrEmpty(Solution?.ServiceOrderSolution))
            throw new EntityException<ServiceOrder>("A solução não pode estar nula ao adicionar a rejeição");

        RepairStatus = ERepairStatus.Disapproved;
        ResponseDate = DateTime.UtcNow;
    }

    public void AddPurchasedPart()
    {
        ServiceOrderStatus = EServiceOrderStatus.OrderPart;
        PurchasePartDate = DateTime.UtcNow;
    }

    public void ExecuteRepair()
    {
        if (RepairStatus == ERepairStatus.Entered || RepairStatus == ERepairStatus.Waiting)
            throw new EntityException<ServiceOrder>("Não podemos executar o conserto pois a ordem de serviço não tem o status de aprovado");

        if (RepairStatus == ERepairStatus.Waiting)
            throw new EntityException<ServiceOrder>("Não podemos executar o conserto pois a estamos aguardando a resposta do cliente");


        ServiceOrderStatus = EServiceOrderStatus.Repaired;
        RepairDate = DateTime.UtcNow;
    }

    public void AddDelivery()
    {
        ServiceOrderStatus = EServiceOrderStatus.Delivered;
        DeliveryDate = DateTime.UtcNow;
    }

    public void UpdateServiceOrder(
        Customer customer,
        string productBrand,
        string productModel,
        string productSerialNumber,
        string productDefect,
        string accessories,
        EProduct productType,
        EEnterprise enterprise)
    {
        CustomerId = customer.Id;
        Customer = customer;
        Product.UpdateProduct(productBrand, productModel,
            productSerialNumber, productDefect, accessories, productType);
        Enterprise = enterprise;
    }

    public void UpdateCustomer(Customer customer)
    {
        if (customer.Id == 0 || string.IsNullOrEmpty(customer.Name.CustomerName))
            throw new EntityException<ServiceOrder>("O campo ID e nome do cliente estão vazios");

        Customer = customer;
    }

    private string GenerateRandomCode()
    {
        const string letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
        const string digits = "0123456789";

        Random random = new Random();

        char letter1 = letters[random.Next(letters.Length)];
        char digit1 = digits[random.Next(digits.Length)];
        char letter2 = letters[random.Next(letters.Length)];
        char digit2 = digits[random.Next(digits.Length)];

        return $"{letter1}{digit1}{letter2}{digit2}".ToUpper();
    }

    public void EditCustomer(Customer customer)
    {
        if (customer.Id == 0 || string.IsNullOrEmpty(customer.Name.CustomerName))
            throw new EntityException<ServiceOrder>("O campo ID e nome do cliente estão vazios");
        
        Customer = customer;
    }

    public void EditProduct(Product product)
    {
        Product.UpdateProduct(
            product.Brand, product.Model, product.SerialNumber, product.Defect ?? "",
            product.Accessories ?? "", product.Type);
    }

    public void EditEnterprise(EEnterprise enterprise)
    {
        Enterprise = enterprise;
    }
    
    public void EditEnterprise(DateTime deliveryDate)
    {
        DeliveryDate = deliveryDate;
    }

    public void EditSolution(Solution solution)
    {
        Solution = solution;
    }

    public void EditGuarantee(Guarantee guarantee)
    {
        Guarantee = guarantee;
    }

    public void EditPartCost(PartCost partCost)
    {
        PartCost = partCost;
    }

    public void EditLaborCost(LaborCost laborCost)
    {
        LaborCost = laborCost;
    }

    public void EditServiceOrderStatus(EServiceOrderStatus serviceOrderStatus)
    {
        ServiceOrderStatus = serviceOrderStatus;
    }

    public void EditRepairStatus(ERepairStatus repairStatus)
    {
        RepairStatus = repairStatus;
    }

    public void EditRepairResult(ERepairResult repairResult)
    {
        RepairResult = repairResult;
    }

    public void EditDeliveryDate(DateTime? deliveryDate)
    {
        DeliveryDate = deliveryDate;
    }

    public void EditEstimateMessage(string message)
    {
        EstimateMessage = message;
    }
}