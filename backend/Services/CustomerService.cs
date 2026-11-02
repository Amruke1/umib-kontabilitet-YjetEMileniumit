using System.ComponentModel.DataAnnotations;
using MiniERP.Models;

namespace MiniERP.Services;

public sealed class CustomerService
{
    private readonly object gate = new();
    private readonly List<Customer> customers = [];
    private int nextId = 1;

    public IReadOnlyList<Customer> GetAll()
    {
        lock (gate)
            return customers.Select(Copy).ToArray();
    }

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
        lock (gate)
        {
            customer.Id = checked(nextId++);
            customers.Add(customer);
            return Copy(customer);
        }
    }

    private static Customer Copy(Customer customer) => new()
    {
        Id = customer.Id, Name = customer.Name, Email = customer.Email, Phone = customer.Phone
    };
}
