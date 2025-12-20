using System.Net.Http.Json;
using TVS_App.Application.DTOs;
using TVS_App.Application.DTOs.ServiceOrder;
using TVS_App.Application.Requests.ServiceOrderRequests.ServiceOrderCommands;
using TVS_App.Application.Requests.ServiceOrderRequests.ServiceOrdersQueries;
using TVS_App.Domain.Entities;
using TVS_App.Domain.Enums;
using TVS_App.Domain.Shared;

namespace TVSApp.Web.Handlers;

public class ServiceOrderHandler
{
    private readonly HttpClient _httpClient;
    
    public ServiceOrderHandler(IHttpClientFactory clientFactory)
    {
        _httpClient = clientFactory.CreateClient("api");
    }

    public async Task<BaseResponse<byte[]>> CreateServiceOrderAndReturnPdfAsync(CreateServiceOrderCommand command)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("create-service-order", command);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return new BaseResponse<byte[]>(null, (int)response.StatusCode, $"Erro: {error}");
            }

            var pdfBytes = await response.Content.ReadAsByteArrayAsync();
            return new BaseResponse<byte[]>(pdfBytes, 200, "PDF gerado com sucesso");
        }
        catch
        {
            return new BaseResponse<byte[]>(null, 500, "Erro ao se conectar ao servidor.");
        }
    }

    public async Task<BaseResponse<byte[]>> CreateSalesServiceOrderAsync(CreateSalesServiceOrderCommand command)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("create-sales-service-order", command);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return new BaseResponse<byte[]>(null, (int)response.StatusCode, $"Erro: {error}");
            }

            var pdfBytes = await response.Content.ReadAsByteArrayAsync();
            return new BaseResponse<byte[]>(pdfBytes, 200, "PDF gerado com sucesso");
        }
        catch
        {
            return new BaseResponse<byte[]>(null, 500, "Erro ao se conectar ao servidor.");
        }
    }
    
    public async Task<BaseResponse<UpdateServiceOrderResponseDto?>> EditServiceOrderAsync(EditServiceOrderCommand command)
    {
        try
        {
            if (command.Id == 0)
                return new BaseResponse<UpdateServiceOrderResponseDto?>(null, 401, "O ID da ordem de serviço nao pode ser 0");
                
            var response = await _httpClient.PutAsJsonAsync("edit-service-order", command);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return new BaseResponse<UpdateServiceOrderResponseDto?>(null, (int)response.StatusCode, 
                    $"Erro na API: {response.StatusCode}. Detalhes: {errorContent}");
            }
            
            var content = await response.Content.ReadFromJsonAsync<BaseResponse<UpdateServiceOrderResponseDto?>>();
            if (content == null || !content.IsSuccess)
                return new BaseResponse<UpdateServiceOrderResponseDto?>(null, 500, content?.Message);
            
            return content;
        }
        catch
        {
            return new BaseResponse<UpdateServiceOrderResponseDto?>(null, 500, "Erro ao se conectar ao servidor.");
        }
    }

    public async Task<BaseResponse<ServiceOrderDto?>> GetServiceOrderById(GetServiceOrderByIdQuery command)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<BaseResponse<ServiceOrderDto?>>(
                $"get-service-order-by-id/{command.Id}");
            
            if (response == null || !response.IsSuccess)
                return new BaseResponse<ServiceOrderDto?>(null, 500, response?.Message ?? "");
            
            return response;
        }
        catch
        {
            return new BaseResponse<ServiceOrderDto?>(null, 500, "Erro ao se conectar ao servidor.");
        }
    }

    public async Task<BaseResponse<List<ServiceOrderDto>>> GetServiceOrdersByCustomerName(string customerName)
    {
        try
        {
            var encodedName = Uri.EscapeDataString(customerName);
            var response = await _httpClient.GetFromJsonAsync<BaseResponse<List<ServiceOrderDto>>>(
                $"get-service-orders-by-customer-name?name={encodedName}");
            
            if (response == null || !response.IsSuccess)
                return new BaseResponse<List<ServiceOrderDto>>(null, 500, response?.Message);
            
            return response;
        }
        catch
        {
            return new BaseResponse<List<ServiceOrderDto>>(null, 500, "Erro ao se conectar ao servidor.");
        }
    }
    
    public async Task<BaseResponse<List<ServiceOrderDto>>> GetServiceOrdersBySerialNumber(string serialNumber)
    {
        try
        {
            var encodedName = Uri.EscapeDataString(serialNumber);
            var response = await _httpClient.GetFromJsonAsync<BaseResponse<List<ServiceOrderDto>>>(
                $"get-service-orders-by-serial-number?serialNumber={encodedName}");
            
            if (response == null || !response.IsSuccess)
                return new BaseResponse<List<ServiceOrderDto>>(null, 500, response?.Message);
            
            return response;
        }
        catch
        {
            return new BaseResponse<List<ServiceOrderDto>>(null, 500, "Erro ao se conectar ao servidor.");
        }
    }
    
    public async Task<BaseResponse<List<ServiceOrderDto>>> GetServiceOrdersByModel(string model)
    {
        try
        {
            var encodedName = Uri.EscapeDataString(model);
            var response = await _httpClient.GetFromJsonAsync<BaseResponse<List<ServiceOrderDto>>>(
                $"get-service-orders-by-model?model={encodedName}");
            
            if (response == null || !response.IsSuccess)
                return new BaseResponse<List<ServiceOrderDto>>(null, 500, response?.Message);
            
            return response;
        }
        catch
        {
            return new BaseResponse<List<ServiceOrderDto>>(null, 500, "Erro ao se conectar ao servidor.");
        }
    }
    
    public async Task<BaseResponse<List<ServiceOrderDto>>> GetServiceOrdersByEnterprise(EEnterprise enterprise)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<BaseResponse<List<ServiceOrderDto>>>(
                $"get-service-orders-by-enterprise?enterprise={enterprise}");
            
            if (response == null || !response.IsSuccess)
                return new BaseResponse<List<ServiceOrderDto>>(null, 500, response?.Message);
            
            return response;
        }
        catch
        {
            return new BaseResponse<List<ServiceOrderDto>>(null, 500, "Erro ao se conectar ao servidor.");
        }
    }
    
    public async Task<BaseResponse<List<ServiceOrderDto>>> GetServiceOrdersByDate(DateTime startDate, DateTime endDate)
    {
        try
        {
            var start = Uri.EscapeDataString(startDate.ToString("yyyy-MM-dd"));
            var end = Uri.EscapeDataString(endDate.ToString("yyyy-MM-dd"));
            
            var response = await _httpClient.GetFromJsonAsync<BaseResponse<List<ServiceOrderDto>>>(
                $"get-service-orders-by-date?startDate={start}&endDate={end}");
            
            if (response == null || !response.IsSuccess)
                return new BaseResponse<List<ServiceOrderDto>>(null, 500, response?.Message);
            
            return response;
        }
        catch
        {
            return new BaseResponse<List<ServiceOrderDto>>(null, 500, "Erro ao se conectar ao servidor.");
        }
    }

    public async Task<BaseResponse<PaginatedResult<ServiceOrderDto?>>> GetAllServiceOrdersAsync(GetAllServiceOrdersQuery query)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<BaseResponse<PaginatedResult<ServiceOrderDto?>>>(
                $"get-all-service-orders/{query.PageNumber}/{query.PageSize}");
            
            if (response == null || !response.IsSuccess)
                return new BaseResponse<PaginatedResult<ServiceOrderDto?>>(null, 500, response?.Message);
            
            return response;
        }
        catch
        {
            return new BaseResponse<PaginatedResult<ServiceOrderDto?>>(null, 500, "Erro ao se conectar ao servidor.");
        }
    }

    public async Task<BaseResponse<PaginatedResult<ServiceOrderDto?>>> GetPendingEstimatesAsync(GetPendingEstimatesQuery query)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<BaseResponse<PaginatedResult<ServiceOrderDto?>>>(
                $"get-pending-estimates-service-orders/{query.PageNumber}/{query.PageSize}");
            
            if (response == null || !response.IsSuccess)
                return new BaseResponse<PaginatedResult<ServiceOrderDto?>>(null, 500, response?.Message);
            
            return response;
        }
        catch
        {
            return new BaseResponse<PaginatedResult<ServiceOrderDto?>>(null, 500, "Erro ao se conectar ao servidor.");
        }
    }

    public async Task<BaseResponse<PaginatedResult<ServiceOrderDto?>>> GetWaitingResponseAsync(GetWaitingResponseQuery query)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<BaseResponse<PaginatedResult<ServiceOrderDto?>>>(
                $"get-waiting-response-service-orders/{query.PageNumber}/{query.PageSize}");
            
            if (response == null || !response.IsSuccess)
                return new BaseResponse<PaginatedResult<ServiceOrderDto?>>(null, 500, response?.Message);
            
            return response;
        }
        catch
        {
            return new BaseResponse<PaginatedResult<ServiceOrderDto?>>(null, 500, "Erro ao se conectar ao servidor.");
        }
    }
    
    public async Task<BaseResponse<PaginatedResult<ServiceOrderDto?>>> GetPendingPurchasePartAsync(GetPendingPurchasePartQuery query)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<BaseResponse<PaginatedResult<ServiceOrderDto?>>>(
                $"get-pending-purchase-service-orders/{query.PageNumber}/{query.PageSize}");
            
            if (response == null || !response.IsSuccess)
                return new BaseResponse<PaginatedResult<ServiceOrderDto?>>(null, 500, response?.Message);
            
            return response;
        }
        catch
        {
            return new BaseResponse<PaginatedResult<ServiceOrderDto?>>(null, 500, "Erro ao se conectar ao servidor.");
        }
    }
    
    public async Task<BaseResponse<PaginatedResult<ServiceOrderDto?>>> GetRejectedEstimateServiceOrdersAsync(GetRejectedEstimateServiceOrdersQuery query)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<BaseResponse<PaginatedResult<ServiceOrderDto?>>>(
                $"get-rejected-estimate-service-orders/{query.PageNumber}/{query.PageSize}");
            
            if (response == null || !response.IsSuccess)
                return new BaseResponse<PaginatedResult<ServiceOrderDto?>>(null, 500, response?.Message);
            
            return response;
        }
        catch
        {
            return new BaseResponse<PaginatedResult<ServiceOrderDto?>>(null, 500, "Erro ao se conectar ao servidor.");
        }
    }
    
    public async Task<BaseResponse<PaginatedResult<ServiceOrderDto?>>> GetWaitingPartsAsync(GetWaitingPartsQuery query)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<BaseResponse<PaginatedResult<ServiceOrderDto?>>>(
                $"get-waiting-parts-service-orders/{query.PageNumber}/{query.PageSize}");
            
            if (response == null || !response.IsSuccess)
                return new BaseResponse<PaginatedResult<ServiceOrderDto?>>(null, 500, response?.Message);
            
            return response;
        }
        catch
        {
            return new BaseResponse<PaginatedResult<ServiceOrderDto?>>(null, 500, "Erro ao se conectar ao servidor.");
        }
    }
    
    public async Task<BaseResponse<PaginatedResult<ServiceOrderDto?>>> GetWaitingPickupAsync(GetWaitingPickupQuery query)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<BaseResponse<PaginatedResult<ServiceOrderDto?>>>(
                $"get-waiting-pickup-service-orders/{query.PageNumber}/{query.PageSize}");
            
            if (response == null || !response.IsSuccess)
                return new BaseResponse<PaginatedResult<ServiceOrderDto?>>(null, 500, response?.Message);
            
            return response;
        }
        catch
        {
            return new BaseResponse<PaginatedResult<ServiceOrderDto?>>(null, 500, "Erro ao se conectar ao servidor.");
        }
    }
    
    public async Task<BaseResponse<PaginatedResult<ServiceOrderDto?>>> GetDeliveredAsync(GetDeliveredQuery query)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<BaseResponse<PaginatedResult<ServiceOrderDto?>>>(
                $"get-delivered-service-orders/{query.PageNumber}/{query.PageSize}");
            
            if (response == null || !response.IsSuccess)
                return new BaseResponse<PaginatedResult<ServiceOrderDto?>>(null, 500, response?.Message);
            
            return response;
        }
        catch
        {
            return new BaseResponse<PaginatedResult<ServiceOrderDto?>>(null, 500, "Erro ao se conectar ao servidor.");
        }
    }

    public async Task<BaseResponse<string?>> AddProductLocation(AddProductLocationCommand command)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync("add-product-location", command);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return new BaseResponse<string?>(null, (int)response.StatusCode, 
                    $"Erro na API: {response.StatusCode}. Detalhes: {errorContent}");
            }
            
            var content = await response.Content.ReadFromJsonAsync<BaseResponse<string?>>();
            if (content == null || !content.IsSuccess)
                return new BaseResponse<string?>(null, 500, content?.Message);
            
            return content;
        }
        catch
        {
            return new BaseResponse<string?>(null, 500, "Erro ao se conectar ao servidor.");
        }
    }

    public async Task<BaseResponse<ServiceOrderDto?>> AddServiceOrderEstimate(AddServiceOrderEstimateCommand command)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync("add-service-order-estimate", command);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return new BaseResponse<ServiceOrderDto?>(null, (int)response.StatusCode, 
                    $"Erro na API: {response.StatusCode}. Detalhes: {errorContent}");
            }
            
            var content = await response.Content.ReadFromJsonAsync<BaseResponse<ServiceOrderDto?>>();
            if (content == null || !content.IsSuccess)
                return new BaseResponse<ServiceOrderDto?>(null, 500, content?.Message);
            
            return content;
        }
        catch
        {
            return new BaseResponse<ServiceOrderDto?>(null, 500, "Erro ao se conectar ao servidor.");
        }
    }
    
    public async Task<BaseResponse<ServiceOrderDto?>> AddServiceOrderApproveEstimate(AddServiceOrderApproveEstimateCommand command)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync("add-service-order-approve-estimate", command);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return new BaseResponse<ServiceOrderDto?>(null, (int)response.StatusCode, 
                    $"Erro na API: {response.StatusCode}. Detalhes: {errorContent}");
            }
            
            var content = await response.Content.ReadFromJsonAsync<BaseResponse<ServiceOrderDto?>>();
            if (content == null || !content.IsSuccess)
                return new BaseResponse<ServiceOrderDto?>(null, 500, content?.Message);
            
            return content;
        }
        catch
        {
            return new BaseResponse<ServiceOrderDto?>(null, 500, "Erro ao se conectar ao servidor.");
        }
    }
    
    public async Task<BaseResponse<ServiceOrderDto?>> AddServiceOrderRejectEstimate(AddServiceOrderRejectEstimateCommand command)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync("add-service-order-reject-estimate", command);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return new BaseResponse<ServiceOrderDto?>(null, (int)response.StatusCode, 
                    $"Erro na API: {response.StatusCode}. Detalhes: {errorContent}");
            }
            
            var content = await response.Content.ReadFromJsonAsync<BaseResponse<ServiceOrderDto?>>();
            if (content == null || !content.IsSuccess)
                return new BaseResponse<ServiceOrderDto?>(null, 500, content?.Message);
            
            return content;
        }
        catch
        {
            return new BaseResponse<ServiceOrderDto?>(null, 500, "Erro ao se conectar ao servidor.");
        }
    }
    
    public async Task<BaseResponse<ServiceOrderDto?>> MarkRejectedEstimateAsReadyToPickUp(MarkEstimateRejectedAsReadyToPickUpCommand command)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync("mark-estimate-rejected-as-ready-to-pick-up", command);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return new BaseResponse<ServiceOrderDto?>(null, (int)response.StatusCode, 
                    $"Erro na API: {response.StatusCode}. Detalhes: {errorContent}");
            }
            
            var content = await response.Content.ReadFromJsonAsync<BaseResponse<ServiceOrderDto?>>();
            if (content == null || !content.IsSuccess)
                return new BaseResponse<ServiceOrderDto?>(null, 500, content?.Message);
            
            return content;
        }
        catch
        {
            return new BaseResponse<ServiceOrderDto?>(null, 500, "Erro ao se conectar ao servidor.");
        }
    }

    public async Task<BaseResponse<ServiceOrderDto?>> AddPurchasedPart(AddPurchasedPartCommand command)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync("add-service-order-purchased-part", command);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return new BaseResponse<ServiceOrderDto?>(null, (int)response.StatusCode,
                    $"Erro na API: {response.StatusCode}. Detalhes: {errorContent}");
            }

            var content = await response.Content.ReadFromJsonAsync<BaseResponse<ServiceOrderDto?>>();
            if (content == null || !content.IsSuccess)
                return new BaseResponse<ServiceOrderDto?>(null, 500, content?.Message);

            return content;
        }
        catch
        {
            return new BaseResponse<ServiceOrderDto?>(null, 500, "Erro ao se conectar ao servidor.");
        }
    }

    public async Task<BaseResponse<ServiceOrderDto?>> AddServiceOrderRepair(AddServiceOrderRepairCommand command)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync("add-service-order-repair", command);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return new BaseResponse<ServiceOrderDto?>(null, (int)response.StatusCode,
                    $"Erro na API: {response.StatusCode}. Detalhes: {errorContent}");
            }

            var content = await response.Content.ReadFromJsonAsync<BaseResponse<ServiceOrderDto?>>();
            if (content == null || !content.IsSuccess)
                return new BaseResponse<ServiceOrderDto?>(null, 500, content?.Message);

            return content;
        }
        catch
        {
            return new BaseResponse<ServiceOrderDto?>(null, 500, "Erro ao se conectar ao servidor.");
        }
    }

    public async Task<BaseResponse<byte[]?>> AddServiceOrderDeliveryAndReturnPdfAsync(AddServiceOrderDeliveryAndReturnPdfCommand command)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync("add-service-order-delivery", command);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return new BaseResponse<byte[]?>(null, (int)response.StatusCode, $"Erro: {error}");
            }

            var pdfBytes = await response.Content.ReadAsByteArrayAsync();
            if (pdfBytes.Length > 0)
                return new BaseResponse<byte[]?>(pdfBytes, 200, "PDF gerado com sucesso");

            return new BaseResponse<byte[]?>(null, 200, "Ordem de serviço entregue com sucesso");
        }
        catch
        {
            return new BaseResponse<byte[]?>(null, 500, "Erro ao se conectar ao servidor.");
        }
    }

    public async Task<BaseResponse<byte[]>> RegenerateAndReturnPdfAsync(RegenerateAndReturnPdfCommand command)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("regenerate-service-order-pdf", command);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return new BaseResponse<byte[]>(null, (int)response.StatusCode, $"Erro: {error}");
            }

            var pdfBytes = await response.Content.ReadAsByteArrayAsync();
            return new BaseResponse<byte[]>(pdfBytes, 200, "PDF gerado com sucesso");
        }
        catch
        {
            return new BaseResponse<byte[]>(null, 500, "Erro ao se conectar ao servidor.");
        }
    }

    public async Task<BaseResponse<List<Payment>>> GetPaymentsAsync(long serviceOrderId)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<BaseResponse<List<Payment>>>(
                $"get-payments?serviceOrderId={serviceOrderId}");

            if (response == null || !response.IsSuccess)
                return new BaseResponse<List<Payment>>(null, 500, response?.Message ?? "Erro ao buscar pagamentos");

            return response;
        }
        catch
        {
            return new BaseResponse<List<Payment>>(null, 500, "Erro ao se conectar ao servidor.");
        }
    }

    public async Task<BaseResponse<string>> AddPaymentAsync(AddPaymentCommand command)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("add-payment", command);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return new BaseResponse<string>(null, (int)response.StatusCode,
                    $"Erro na API: {response.StatusCode}. Detalhes: {errorContent}");
            }

            var content = await response.Content.ReadFromJsonAsync<BaseResponse<string>>();
            if (content == null || !content.IsSuccess)
                return new BaseResponse<string>(null, 500, content?.Message);

            return content;
        }
        catch
        {
            return new BaseResponse<string>(null, 500, "Erro ao se conectar ao servidor.");
        }
    }

    public async Task<BaseResponse<string>> UpdatePaymentAsync(UpdatePaymentCommand command)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync("update-payment", command);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return new BaseResponse<string>(null, (int)response.StatusCode,
                    $"Erro na API: {response.StatusCode}. Detalhes: {errorContent}");
            }

            var content = await response.Content.ReadFromJsonAsync<BaseResponse<string>>();
            if (content == null || !content.IsSuccess)
                return new BaseResponse<string>(null, 500, content?.Message);

            return content;
        }
        catch
        {
            return new BaseResponse<string>(null, 500, "Erro ao se conectar ao servidor.");
        }
    }

    public async Task<BaseResponse<string>> RemovePaymentAsync(RemovePaymentCommand command)
    {
        try
        {
            var response = await _httpClient.DeleteAsync(
                $"remove-payment?serviceOrderId={command.ServiceOrderId}&paymentId={command.PaymentId}");

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return new BaseResponse<string>(null, (int)response.StatusCode,
                    $"Erro na API: {response.StatusCode}. Detalhes: {errorContent}");
            }

            var content = await response.Content.ReadFromJsonAsync<BaseResponse<string>>();
            if (content == null || !content.IsSuccess)
                return new BaseResponse<string>(null, 500, content?.Message);

            return content;
        }
        catch
        {
            return new BaseResponse<string>(null, 500, "Erro ao se conectar ao servidor.");
        }
    }
}