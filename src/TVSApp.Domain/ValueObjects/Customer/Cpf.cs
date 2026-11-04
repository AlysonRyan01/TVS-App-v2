using TVS_App.Domain.Exceptions;

namespace TVS_App.Domain.ValueObjects.Customer;

public class Cpf : ValueObject
{
    protected Cpf() { }
    
    public Cpf(string number)
    {
        if (string.IsNullOrWhiteSpace(number))
            throw new ValueObjectException<Cpf>("Documento não pode ser vazio");

        number = number.Trim().Replace(".", "").Replace("-", "").Replace("/", "");
        
        if (number.Length != 11)
            throw new ValueObjectException<Cpf>("O CPF deve ter 11 caracteres");
        
        Number = number;
    }
    
    public string Number { get; private set; } = string.Empty;

    public override string ToString() => Convert.ToUInt64(Number).ToString(@"000\.000\.000\-00");
}