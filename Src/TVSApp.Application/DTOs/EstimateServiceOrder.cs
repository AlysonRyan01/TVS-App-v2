using TVS_App.Domain.Entities;

namespace TVS_App.Application.DTOs;

public class EstimateServiceOrder
{
    public EstimateServiceOrder(long id, string customerName, string productBrand, string productModel, string productDefect, string? guarantee, decimal totalAmount, string? estimateMessage)
    {
        Id = id;
        CustomerName = customerName;
        ProductBrand = productBrand;
        ProductModel = productModel;
        ProductDefect = productDefect;
        Guarantee = guarantee;
        TotalAmount = totalAmount;
        EstimateMessage = estimateMessage;
    }

    public long Id { get; set; }
    public string CustomerName { get; set; }
    public string ProductBrand { get; set; }
    public string ProductModel { get; set; }
    public string ProductDefect { get; set; } 
    
    public string? Guarantee { get; set; }
    public decimal TotalAmount { get; set; }
    public string? EstimateMessage { get; set; }
}