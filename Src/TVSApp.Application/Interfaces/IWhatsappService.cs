using TVS_App.Domain.Enums;
using TVS_App.Domain.Shared;

namespace TVS_App.Application.Interfaces;

public interface IWhatsappService
{
    Task<BaseResponse<string>> SendWelcomeMessage(string serviceOrder, string customerName, string phoneNumber);
    Task<BaseResponse<string>> SendEstimateMessage(string serviceOrder, ERepairResult result, string customerName, string solution, decimal amount, string guarantee, string phoneNumber);
    Task<BaseResponse<string>> SendDeviceReadyMessage(string serviceOrder, string customerName, string phoneNumber);
    Task<BaseResponse<string>> SendGuaranteeMessage(string serviceOrder, string customerName, string guarantee, string phoneNumber);
    Task<BaseResponse<string>> SendProductSaleMessage(string customerName, string phoneNumber, decimal amount, string guarantee, EProduct productType);
}