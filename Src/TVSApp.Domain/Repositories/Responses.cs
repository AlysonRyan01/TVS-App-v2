namespace TVS_App.Domain.Repositories;

public record PendingEstimateServiceOrder(
    long Number,
    string Customer,
    string Company,
    string Model,
    string? Defect,
    DateTime EntryDate
);

public record WaitingResponseServiceOrder(
    long Number,
    string Customer,
    string Model,
    string? Solution,
    decimal Amount,
    DateTime? InspectionIn
);

public record ApprovedServiceOrder(
    long Number,
    string Company,
    string Customer,
    string Model,
    string? Solution,
    DateTime? ApprovedIn
);

public record RefusedServiceOrder(
    long Number,
    string Customer,
    string Model,
    string? Solution,
    DateTime? RefusedIn
);

public record WaitingPartServiceOrder(
    long Number,
    string Customer,
    string Model,
    string? Solution,
    DateTime? PurchasedIn
);

public record ReadyServiceOrder(
    long Number,
    string Customer,
    string Model,
    DateTime? RepairDate,
    string? Situation,
    string? Response
);