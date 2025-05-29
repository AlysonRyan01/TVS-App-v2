using TVS_App.Domain.Enums;

namespace TVS_App.Application.Commands.ServiceOrderCommands;

public class EditServiceOrderCommand : ICommand
{
    public long Id { get; set; }
    public long CustomerId { get; set; }
    
    public string Name { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string Neighborhood { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Number { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Phone2 { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string Cpf { get; set; } = string.Empty;
    
    public string Brand { get; set; } = null!;
    public string Model { get; set; } = null!;
    public string SerialNumber { get; set; } = null!;
    public string Defect { get; set; } = string.Empty;
    public string Accessories { get; set; } = string.Empty;
    public EProduct Type { get; set; }
    public string Location { get; set; } = string.Empty;
        
    public EEnterprise Enterprise { get; set; }
    public DateTime? DeliveryDate { get; set; }
    
    public string? Solution { get; set; }
    
    public string? Guarantee { get; set; }
    
    public string? EstimateMessage { get; set; }
    
    public decimal PartCost { get; set; }
    
    public decimal LaborCost { get; set; }
    
    public EServiceOrderStatus ServiceOrderStatus { get; set; }
    
    public ERepairStatus RepairStatus { get; set; }
    
    public ERepairResult? RepairResult { get; set; }

    public void Normalize()
    {
        Name = Name.ToUpper().Trim();
        Street = Street.ToUpper().Trim();
        Neighborhood = Neighborhood.ToUpper().Trim();
        City = City.ToUpper().Trim();
        Number = Number.ToUpper().Trim();
        ZipCode = ZipCode.ToUpper().Trim();
        State = State.ToUpper().Trim();
        Phone = Phone.ToUpper().Trim();
        Phone2 = Phone2.ToUpper().Trim();
        if (!string.IsNullOrEmpty(Email))
            Email = Email.ToUpper().Trim();
        Cpf = Cpf.ToUpper().Trim();
    
        Brand = Brand.ToUpper().Trim();
        Model = Model.ToUpper().Trim();
        SerialNumber = SerialNumber.ToUpper().Trim();
        Defect = Defect.ToUpper().Trim();
        Accessories = Accessories.ToUpper().Trim();
        Location = Location.ToUpper().Trim();
    
        if (!string.IsNullOrEmpty(Solution))
            Solution = Solution.ToUpper().Trim();
    
        if (!string.IsNullOrEmpty(Guarantee))
            Guarantee = Guarantee.ToUpper().Trim();
    
        if (!string.IsNullOrEmpty(EstimateMessage))
            EstimateMessage = EstimateMessage.ToUpper().Trim();
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