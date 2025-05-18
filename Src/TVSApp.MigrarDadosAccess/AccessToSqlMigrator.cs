using System.Data.OleDb;
using TVS_App.Domain.Entities;
using TVS_App.Domain.Enums;
using TVS_App.Domain.ValueObjects.Customer;
using TVS_App.Domain.ValueObjects.ServiceOrder;
using TVS_App.Infrastructure.Data;

namespace TVS_App.MigrarDadosAccess;

public class AccessToSqlMigrator
{
    private readonly string _accessConnStr;
    private readonly ApplicationDataContext _sqlDbContext;

    public AccessToSqlMigrator(string accessConnStr, ApplicationDataContext sqlDbContext)
    {
        _accessConnStr = accessConnStr;
        _sqlDbContext = sqlDbContext;
    }

    public async Task MigrarClientesAsync()
    {
        var customers = new List<Customer>();

        using var connection = new OleDbConnection(_accessConnStr);
        await connection.OpenAsync();

        const string query = @"SELECT 
                            cli_codigo AS [Code],
                            cli_nome AS [Name],
                            cli_endereco AS [Street],
                            cli_bairro AS [Neighborhood],
                            cli_cidade AS [City],
                            cli_numero AS [Number],
                            cli_cep AS [ZipCode],
                            cli_uf AS [State],
                            cli_telefone AS [Phone],
                            cli_celular AS [CellPhone],
                            cli_email AS [Email]
                            FROM cliente ORDER BY cli_codigo ASC";

        using var command = new OleDbCommand(query, connection);
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            try
            {
                var name = new Name(reader["Name"] as string ?? "Não tem");
                var address = new Address(
                    reader["Street"] as string,
                    reader["Neighborhood"] as string,
                    reader["City"] as string,
                    reader["Number"] as string,
                    reader["ZipCode"] as string,
                    reader["State"] as string);

                var phone = new Phone(reader["CellPhone"] as string ?? "Não tem");
                var phone2 = new Phone(reader["Phone"] as string ?? "Não tem");;


                var emailStr = reader["Email"] as string ?? "Não tem";
                var email = new Email(emailStr);

                var customer = new Customer(name, address, phone, phone2, email)
                {
                    Id = 0
                };

                customers.Add(customer);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao migrar cliente: {ex.Message}");
            }
        }

        await _sqlDbContext.Customers.AddRangeAsync(customers);
        await _sqlDbContext.SaveChangesAsync();
        Console.WriteLine("Migracao feita com sucesso!");
    }

    public async Task MigrarOrdensDeServicoAsync()
    {
        var listUselessOrders = new List<ServiceOrder>();

        for (int i = 1; i <= 28001; i++)
        {
            var order = new ServiceOrder(EEnterprise.Particular, 1, new Product("NÃO TEM", "NÃO TEM", "NÃO TEM", "NÃO TEM", "NÃO TEM", EProduct.Tv));
            order.EntryDate = new DateTime(2009, 1, 1);
            order.AddEstimate("NAO TEM", "NAO TEM", 0M, 0M, ERepairResult.NoDefectFound, "");
            order.InspectionDate = new DateTime(2009, 1, 1);
            order.AddDelivery();
            order.DeliveryDate = new DateTime(2009, 1, 1);
            order.Id = 0;
            listUselessOrders.Add(order);
            Console.WriteLine("adicionando OS para popular o banco");
        }

        await _sqlDbContext.ServiceOrders.AddRangeAsync(listUselessOrders);
        await _sqlDbContext.SaveChangesAsync();

        // separar codigo
        var serviceOrders = new List<ServiceOrder>();

        using var connection = new OleDbConnection(_accessConnStr);
        await connection.OpenAsync();

        const string query = @"SELECT 
                                os.os_codigo AS [Id],
                                os.os_situacao AS [eServiceOrderStatus],
                                os.emp_codigo AS [eEnterprise],
                                os.pro_codigo AS [productType],
                                os.os_marca AS [productBrand],
                                os.os_modelo AS [productModel],
                                os.os_ns AS [productSerialNumber],
                                os.os_defeito AS [productDefect],
                                os.os_solucao AS [solution],
                                os.os_valor AS [amount],
                                os.os_data_entrada AS [entryDate],
                                os.os_data_vistoria AS [inspectionDate],
                                os.os_data_concerto AS [repairDate],
                                os.os_data_entrega AS [deliveryDate],
                                os.cli_codigo AS [customerId],
                                os.os_concerto AS [eRepair],
                                os.os_semconserto AS [eUnrepaired],
                                os.os_valpeca AS [partCost],
                                os.os_valmo AS [laborCost]
                            FROM os ORDER BY os.os_codigo ASC";

        int countOsAccess = 0;

        using var command = new OleDbCommand(query, connection);
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            try
            {
                countOsAccess++;

                var customerId = Convert.ToInt64(reader["customerId"]);
                var productType = (EProduct)Convert.ToInt32(reader["productType"]);
                var enterprise = (EEnterprise)Convert.ToInt32(reader["eEnterprise"]);

                var model = reader["productModel"] as string ?? "Não tem";
                var serial = reader["productSerialNumber"] as string ?? "Não tem";
                var defect = reader["productDefect"] as string ?? "Não tem";
                var brand = reader["productBrand"] as string ?? "Não tem";
                var accessories = "Não tem";

                var product = new Product(brand, model, serial, defect, accessories, productType);

                var serviceOrderId = 0;
                var serviceOrder = new ServiceOrder(enterprise, customerId, product) { Id = serviceOrderId };

                if (reader["entryDate"] != DBNull.Value)
                {
                    serviceOrder.EntryDate = Convert.ToDateTime(reader["entryDate"]);
                }

                if (reader["solution"] != DBNull.Value && !string.IsNullOrWhiteSpace(reader["solution"]?.ToString()) &&
                    reader["inspectionDate"] != DBNull.Value && !string.IsNullOrWhiteSpace(reader["inspectionDate"]?.ToString()))
                {
                    var solution = reader["solution"] as string ?? "Não tem";
                    var partCost = reader["partCost"] != DBNull.Value ? Convert.ToDecimal(reader["partCost"]) : 0;
                    var laborCost = reader["laborCost"] != DBNull.Value ? Convert.ToDecimal(reader["laborCost"]) : 0;

                    var repaired = Convert.ToBoolean(reader["eRepair"]);
                    var unrepaired = Convert.ToBoolean(reader["eUnrepaired"]);
                    var repairResult = repaired
                        ? ERepairResult.Repair
                        : unrepaired ? ERepairResult.Unrepaired : ERepairResult.Unrepaired;

                    serviceOrder.AddEstimate(solution, "3 MESES", partCost, laborCost, repairResult, "");
                    serviceOrder.InspectionDate = Convert.ToDateTime(reader["inspectionDate"]);
                }

                var status = (EServiceOrderStatus)Convert.ToInt32(reader["eServiceOrderStatus"]);
                var repairDate = reader["repairDate"] != DBNull.Value ? Convert.ToDateTime(reader["repairDate"]) : (DateTime?)null;
                var deliveryDate = reader["deliveryDate"] != DBNull.Value ? Convert.ToDateTime(reader["deliveryDate"]) : (DateTime?)null;

                if (status == EServiceOrderStatus.Repaired || repairDate.HasValue)
                {
                    if (serviceOrder.Solution == null || string.IsNullOrEmpty(serviceOrder.Solution.ServiceOrderSolution))
                        serviceOrder.AddEstimate("Não tem", "3 MESES", serviceOrder.PartCost.ServiceOrderPartCost, serviceOrder.LaborCost.ServiceOrderLaborCost, serviceOrder.RepairResult ?? ERepairResult.Repair, "");
                    serviceOrder.ApproveEstimate();
                    serviceOrder.ResponseDate = serviceOrder.EntryDate;
                    serviceOrder.AddPurchasedPart();
                    serviceOrder.PurchasePartDate = serviceOrder.ResponseDate;
                    serviceOrder.ExecuteRepair();
                    serviceOrder.RepairDate = Convert.ToDateTime(reader["repairDate"]);
                }

                if (status == EServiceOrderStatus.Delivered || deliveryDate.HasValue)
                {
                    serviceOrder.AddDelivery();
                    serviceOrder.DeliveryDate = Convert.ToDateTime(reader["deliveryDate"]);
                }

                if (reader["inspectionDate"] != DBNull.Value)
                {
                    serviceOrder.InspectionDate = Convert.ToDateTime(reader["inspectionDate"]);
                }

                if (reader["repairDate"] != DBNull.Value)
                {
                    serviceOrder.RepairDate = Convert.ToDateTime(reader["repairDate"]);
                }

                if (reader["deliveryDate"] != DBNull.Value)
                {
                    serviceOrder.DeliveryDate = Convert.ToDateTime(reader["deliveryDate"]);
                }

                if (!_sqlDbContext.Customers.Any(c => c.Id == serviceOrder.CustomerId))
                {
                    Console.WriteLine($"Erro: CustomerId {serviceOrder.CustomerId} da OS {serviceOrder.Id} não existe no SQL Server. OS ignorada.");
                    continue;
                }
                
                serviceOrders.Add(serviceOrder);
                Console.WriteLine($"OS: {serviceOrder.Id} com custumerID: {serviceOrder.CustomerId} adicionada com sucesso!");
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao migrar OS: {ex.Message}");
            }
        }

        using var transaction = await _sqlDbContext.Database.BeginTransactionAsync();
        try
        {
            await _sqlDbContext.ServiceOrders.AddRangeAsync(serviceOrders);
            await _sqlDbContext.SaveChangesAsync();
            await transaction.CommitAsync();
            Console.WriteLine("Migração concluída com sucesso!");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            Console.WriteLine($"Erro durante a migração: {ex.Message}");
        }

        Console.WriteLine($"Total de OS no Access: {countOsAccess}");
        Console.WriteLine($"Total de OS no SQL: {serviceOrders.Count}");
        Console.WriteLine("Migração de ordens de serviço concluída!");
    }
}