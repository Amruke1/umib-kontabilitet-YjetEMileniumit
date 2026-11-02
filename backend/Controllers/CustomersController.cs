using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using MiniERP.Models;
using MiniERP.Services;

namespace MiniERP.Controllers;

[ApiController]
[Route("api/customers")]
public sealed class CustomersController(CustomerService service) : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(service.GetAll());

    [HttpPost]
    public IActionResult Post(CreateCustomerRequest request)
    {
        try
        {
            var customer = service.Add(new Customer
            {
                Name = request.Name, Email = request.Email, Phone = request.Phone
            });
            return StatusCode(StatusCodes.Status201Created, customer);
        }
        catch (ValidationException exception)
        {
            ModelState.AddModelError("customer", exception.Message);
            return ValidationProblem(ModelState);
        }
    }
}

// No ID in the input contract: identity is always assigned by the service.
public sealed class CreateCustomerRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
}
