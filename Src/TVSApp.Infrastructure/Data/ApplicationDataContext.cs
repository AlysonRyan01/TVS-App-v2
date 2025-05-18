using System.Reflection;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TVS_App.Domain.Entities;
using TVS_App.Infrastructure.Models;

namespace TVS_App.Infrastructure.Data;

public class ApplicationDataContext : IdentityDbContext<User>
{
    public ApplicationDataContext(DbContextOptions<ApplicationDataContext> options) : base(options)
    {

    }

    public DbSet<Customer> Customers { get; set; } = null!;
    public DbSet<ServiceOrder> ServiceOrders { get; set; } = null!;
    public DbSet<Notification> Notifications { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(modelBuilder);
    }
}
