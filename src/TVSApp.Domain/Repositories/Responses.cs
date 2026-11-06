using TVS_App.Domain.Enums;

namespace TVS_App.Domain.Repositories;

public record PendingEstimateServiceOrder(
    long Number,
    string Customer,
    string Company,
    string Model,
    string? Defect,
    DateTime EnteredAt
);

public record AwaitingResponseServiceOrder(
    long Number,
    string Customer,
    string Model,
    string? Solution,
    decimal Amount,
    DateTime? EvaluatedAt
);

public record ApprovedServiceOrder(
    long Number,
    string Company,
    string Customer,
    string Model,
    string? Solution,
    DateTime? ApprovedAt
);

public record RejectedServiceOrder(
    long Number,
    string Customer,
    string Model,
    string? Solution,
    DateTime? RejectedAt
);

public record AwaitingPartServiceOrder(
    long Number,
    string Customer,
    string Model,
    string? Solution,
    DateTime? PartPurchasedAt
);

public record ReadyForPickupServiceOrder(
    long Number,
    string Customer,
    string Model,
    DateTime? RepairedAt,
    string? RepairResult,
    string? RepairStatus
);

/// <summary>Linha da tela "Buscar OS". IsSale = OS de venda (defeito "VENDA").</summary>
public record ServiceOrderSearchItem(
    long Number,
    string Customer,
    EProduct ProductType,
    string Brand,
    string Model,
    string SerialNumber,
    EEnterprise Enterprise,
    EServiceOrderStatus ServiceOrderStatus,
    bool IsSale,
    DateTime EnteredAt
);
