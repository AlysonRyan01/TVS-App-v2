namespace TVS_App.Application.Queries.ServiceOrdersQueries;

public record GetServiceOrdersByDateQuery(DateTime StartDate, DateTime EndDate);