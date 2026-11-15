using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using MiniERP.Data;
using MiniERP.Models;

namespace MiniERP.Services;

public sealed class CustomerService(AppDbContext db)
{
    public IReadOnlyList<Customer> GetAll() => db.Customers
        .AsNoTracking()
        .OrderBy(customer => customer.Id)
        .ToArray();

    public Customer Add(Customer input)
    {
        var customer = Normalize(input);
        db.Customers.Add(customer);
        db.SaveChanges();
        return customer;
    }

    public Customer? Get(int id) => db.Customers.AsNoTracking().SingleOrDefault(c => c.Id == id);

    public Customer? Update(int id, Customer input)
    {
        var customer = db.Customers.Find(id);
        if (customer is null) return null;
        var normalized = Normalize(input);
        customer.Name = normalized.Name;
        customer.Email = normalized.Email;
        customer.Phone = normalized.Phone;
        db.SaveChanges();
        return customer;
    }

    public bool Delete(int id)
    {
        var customer = db.Customers.Find(id);
        if (customer is null) return false;
        db.Customers.Remove(customer);
        db.SaveChanges();
        return true;
    }

    private static Customer Normalize(Customer input)
    {
        var customer = new Customer
        {
            Name = input.Name?.Trim() ?? string.Empty,
            Email = string.IsNullOrWhiteSpace(input.Email) ? null : input.Email.Trim(),
            Phone = string.IsNullOrWhiteSpace(input.Phone) ? null : input.Phone.Trim()
        };
        Validator.ValidateObject(customer, new ValidationContext(customer), true);
        return customer;
    }
}
