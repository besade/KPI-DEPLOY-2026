using Microsoft.AspNetCore.Mvc;
using MyWebApp.Data;

namespace MyWebApp.Controllers;

[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    private readonly Database _db;

    public HealthController(Database db) => _db = db;

    /// GET /health/alive — always 200 OK
    [HttpGet("alive")]
    public IActionResult Alive() => Ok("OK");

    /// GET /health/ready — 200 if DB reachable, 500 otherwise
    [HttpGet("ready")]
    public async Task<IActionResult> Ready()
    {
        if (await _db.IsReadyAsync())
            return Ok("OK");
        return StatusCode(500, "Database connection unavailable");
    }
}