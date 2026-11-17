using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniERP.Data;
using Npgsql;

namespace MiniERP.Controllers;

[ApiController]
[Route("health")]
public class HealthController(AppDbContext db, ILogger<HealthController> logger) : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new { status = "ok" });

    [HttpGet("ready")]
    public async Task<IActionResult> Ready(CancellationToken cancellationToken)
    {
        try
        {
            await db.Customers.AsNoTracking().Select(customer => customer.Id)
                .Take(1).ToArrayAsync(cancellationToken);
            return Ok(new { status = "ready" });
        }
        catch (Exception exception) when (exception is NpgsqlException or TimeoutException)
        {
            logger.LogWarning("Database readiness check failed ({FailureType}).", exception.GetType().Name);
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { status = "unavailable" });
        }
    }
}
