using MediatR;
using TVS_App.Application.DTOs.ServiceOrder;
using TVS_App.Domain.Enums;
using TVS_App.Domain.Shared;

namespace TVS_App.Application.Requests.ServiceOrderRequests.ServiceOrderCommands;

public record EditServiceOrderCommand(
    long Id,
    long CustomerId,
    string Name,
    string Street,
    string Neighborhood,
    string City,
    string Number,
    string ZipCode,
    string State,
    string Phone,
    string Phone2,
    string? Email,
    string Cpf,
    string Brand,
    string Model,
    string SerialNumber,
    string Defect,
    string Accessories,
    EProduct Type,
    string Location,
    EEnterprise Enterprise,
    DateTime? EntryDate,
    DateTime? InspectionDate,
    DateTime? ResponseDate,
    DateTime? RepairDate,
    DateTime? PurchasePartDate,
    DateTime? DeliveryDate,
    string? Solution,
    string? Guarantee,
    string? EstimateMessage,
    decimal PartCost,
    decimal LaborCost,
    EServiceOrderStatus ServiceOrderStatus,
    ERepairStatus RepairStatus,
    ERepairResult? RepairResult
) : IRequest<BaseResponse<UpdateServiceOrderResponseDto?>>
{
    public EditServiceOrderCommand Normalize()
    {
        return this with
        {
            Name = Name.ToUpper().Trim(),
            Street = Street.ToUpper().Trim(),
            Neighborhood = Neighborhood.ToUpper().Trim(),
            City = City.ToUpper().Trim(),
            Number = Number.ToUpper().Trim(),
            ZipCode = ZipCode.ToUpper().Trim(),
            State = State.ToUpper().Trim(),
            Phone = Phone.ToUpper().Trim(),
            Phone2 = Phone2.ToUpper().Trim(),
            Email = string.IsNullOrWhiteSpace(Email) ? null : Email.ToUpper().Trim(),
            Cpf = Cpf.ToUpper().Trim(),
            Brand = Brand.ToUpper().Trim(),
            Model = Model.ToUpper().Trim(),
            SerialNumber = SerialNumber.ToUpper().Trim(),
            Defect = Defect.ToUpper().Trim(),
            Accessories = Accessories.ToUpper().Trim(),
            Location = Location.ToUpper().Trim(),
            Solution = string.IsNullOrWhiteSpace(Solution) ? null : Solution.ToUpper().Trim(),
            Guarantee = string.IsNullOrWhiteSpace(Guarantee) ? null : Guarantee.ToUpper().Trim(),
            EstimateMessage = string.IsNullOrWhiteSpace(EstimateMessage) ? null : EstimateMessage.ToUpper().Trim()
        };
    }

    public void Validate()
    {
        if (Id <= 0)
            throw new ArgumentException("O ID da ordem de serviço deve ser maior que zero.");

        if (CustomerId <= 0)
            throw new ArgumentException("O ID do cliente deve ser maior que zero.");

        if (string.IsNullOrWhiteSpace(Name))
            throw new ArgumentException("O nome do cliente é obrigatório.");

        if (string.IsNullOrWhiteSpace(Phone))
            throw new ArgumentException("Pelo menos um telefone é obrigatório.");

        if (string.IsNullOrWhiteSpace(Cpf))
            throw new ArgumentException("O CPF é obrigatório.");

        if (!Enum.IsDefined(typeof(EProduct), Type))
            throw new ArgumentException("Tipo de produto inválido.");

        if (!Enum.IsDefined(typeof(EEnterprise), Enterprise))
            throw new ArgumentException("Empresa inválida.");

        if (!Enum.IsDefined(typeof(EServiceOrderStatus), ServiceOrderStatus))
            throw new ArgumentException("Status da ordem de serviço inválido.");

        if (!Enum.IsDefined(typeof(ERepairStatus), RepairStatus))
            throw new ArgumentException("Status de reparo inválido.");

        if (RepairResult.HasValue && !Enum.IsDefined(typeof(ERepairResult), RepairResult.Value))
            throw new ArgumentException("Resultado de reparo inválido.");

        if (PartCost < 0)
            throw new ArgumentException("O custo da peça não pode ser negativo.");

        if (LaborCost < 0)
            throw new ArgumentException("O custo de mão de obra não pode ser negativo.");
    }
}
