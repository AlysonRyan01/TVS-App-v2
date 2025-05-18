using TVS_App.Domain.Entities;
using TVS_App.Domain.Enums;

namespace TVS_App.Application.DTOs;

public class EstimateServiceOrder
{
    public EstimateServiceOrder(long id, 
        string customerName,
        EProduct productType,
        string productBrand, 
        string productModel, 
        string productDefect, 
        string? guarantee, 
        decimal totalAmount, 
        string? estimateMessage,
        EServiceOrderStatus status,
        ERepairResult? result,
        ERepairStatus repairStatus,
        EEnterprise enterprise)
    {
        Id = id;
        CustomerName = customerName;
        ProductBrand = productBrand;
        ProductModel = productModel;
        ProductDefect = productDefect;
        Guarantee = guarantee;
        TotalAmount = totalAmount;
        EstimateMessage = estimateMessage;
        Status = status;
        Result = result;
        RepairStatus = repairStatus;
        Enterprise = enterprise;
        ProductType = productType;
    }

    public long Id { get; set; }
    public string CustomerName { get; set; }
    public EProduct ProductType { get; set; }
    public string ProductBrand { get; set; }
    public string ProductModel { get; set; }
    public string ProductDefect { get; set; } 
    
    public string? Guarantee { get; set; }
    public decimal TotalAmount { get; set; }
    public string? EstimateMessage { get; set; }

    public EServiceOrderStatus Status { get; set; }
    public ERepairResult? Result { get; set; }
    public ERepairStatus RepairStatus { get; set; }
    
    public EEnterprise Enterprise { get; set; }
}