using Microsoft.AspNetCore.Mvc;
using MyWebApp.Data;
using MyWebApp.Helpers;

namespace MyWebApp.Controllers;

[ApiController]
[Route("items")]
public class ItemsController : ControllerBase
{
    private readonly Database _db;
    public ItemsController(Database db) => _db = db;

    // ─── GET /items ──────────────────────────────────────────────────────────

    /// Returns list of all items
    [HttpGet]
    public async Task<IActionResult> GetItems()
    {
        var items = await _db.GetItemsAsync();

        if (ContentNegotiator.PrefersHtml(Request))
        {
            var rows = items.Select(i => new[] { i.Id.ToString(), i.Name });
            var table = ContentNegotiator.Table(["ID", "Name"], rows);
            return Content(ContentNegotiator.Page("Inventory Items", table), "text/html; charset=utf-8");
        }

        var json = items.Select(i => new { id = i.Id, name = i.Name });
        return Ok(json);
    }

    // ─── POST /items ──────────────────────────────────────────────────────────

    public record CreateItemRequest(string Name, int Quantity);

    /// Creates a new inventory item
    [HttpPost]
    public async Task<IActionResult> CreateItem([FromBody] CreateItemRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
            return BadRequest("name is required");
        if (req.Quantity < 0)
            return BadRequest("quantity must be >= 0");

        var item = await _db.CreateItemAsync(req.Name.Trim(), req.Quantity);

        if (ContentNegotiator.PrefersHtml(Request))
        {
            var html = ContentNegotiator.Page("Item Created", $"""
                <p>Item created successfully.</p>
                <table border="1" cellpadding="4" cellspacing="0">
                  <tr><th>ID</th><td>{item.Id}</td></tr>
                  <tr><th>Name</th><td>{System.Net.WebUtility.HtmlEncode(item.Name)}</td></tr>
                  <tr><th>Quantity</th><td>{item.Quantity}</td></tr>
                  <tr><th>Created At</th><td>{item.CreatedAt:yyyy-MM-dd HH:mm:ss} UTC</td></tr>
                </table>
                <p><a href="/items">Back to list</a></p>
                """);
            return Content(html, "text/html; charset=utf-8");
        }

        return CreatedAtAction(nameof(GetItemById), new { id = item.Id }, new
        {
            id = item.Id,
            name = item.Name,
            quantity = item.Quantity,
            created_at = item.CreatedAt
        });
    }

    // ─── GET /items/{id} ─────────────────────────────────────────────────────

    /// Returns full details for a single item
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetItemById(int id)
    {
        var item = await _db.GetItemByIdAsync(id);
        if (item is null) return NotFound($"Item {id} not found");

        if (ContentNegotiator.PrefersHtml(Request))
        {
            var html = ContentNegotiator.Page($"Item #{item.Id}", $"""
                <table border="1" cellpadding="4" cellspacing="0">
                  <tr><th>ID</th><td>{item.Id}</td></tr>
                  <tr><th>Name</th><td>{System.Net.WebUtility.HtmlEncode(item.Name)}</td></tr>
                  <tr><th>Quantity</th><td>{item.Quantity}</td></tr>
                  <tr><th>Created At</th><td>{item.CreatedAt:yyyy-MM-dd HH:mm:ss} UTC</td></tr>
                </table>
                <p><a href="/items">Back to list</a></p>
                """);
            return Content(html, "text/html; charset=utf-8");
        }

        return Ok(new
        {
            id = item.Id,
            name = item.Name,
            quantity = item.Quantity,
            created_at = item.CreatedAt
        });
    }
}