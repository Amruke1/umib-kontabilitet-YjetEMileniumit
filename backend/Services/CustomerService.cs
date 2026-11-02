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
        var customer = new Customer
        {
            Name = input.Name,
            Email = string.IsNullOrEmpty(input.Email) ? null : input.Email,
            Phone = string.IsNullOrEmpty(input.Phone) ? null : input.Phone
        };
        Validator.ValidateObject(customer, new ValidationContext(customer), true);
        customer.Name = customer.Name.Trim();
        db.Customers.Add(customer);
        db.SaveChanges();
        return customer;
    }
}
