using Microsoft.EntityFrameworkCore;
using MiniERP.Models;

namespace MiniERP.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var customer = modelBuilder.Entity<Customer>();

        customer.ToTable("customers", "public", table =>
            table.HasCheckConstraint("customers_name_not_blank", "btrim(name) <> ''"));

        customer.HasKey(value => value.Id).HasName("customers_pkey");

        customer.Property(value => value.Id)
            .HasColumnName("id")
            .HasColumnType("integer")
            .UseIdentityByDefaultColumn();

        customer.Property(value => value.Name)
            .HasColumnName("name")
            .HasColumnType("character varying(150)")
            .HasMaxLength(150)
            .IsRequired();

        customer.Property(value => value.Email)
            .HasColumnName("email")
            .HasColumnType("character varying(254)")
            .HasMaxLength(254)
            .IsRequired(false);

        customer.Property(value => value.Phone)
            .HasColumnName("phone")
            .HasColumnType("character varying(30)")
            .HasMaxLength(30)
            .IsRequired(false);
    }
}
