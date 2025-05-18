using TVS_App.Application.Exceptions;

namespace TVS_App.Application.Commands;

public class PaginationCommand : ICommand
{
    public int PageNumber { get; set; }
    public int PageSize { get; set; }

    public void Validate()
    {
        if (PageNumber < 1)
            throw new CommandException<PaginationCommand>("O pageNumber não pode ser menor que 1");

        if (PageSize < 1)
            throw new CommandException<PaginationCommand>("O pageSize não pode ser menor que 1");
    }
}