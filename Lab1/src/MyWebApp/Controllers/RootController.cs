using Microsoft.AspNetCore.Mvc;
using MyWebApp.Helpers;

namespace MyWebApp.Controllers;

[ApiController]
[Route("/")]
public class RootController : ControllerBase
{
    private static readonly (string Method, string Path, string Description)[] Endpoints =
    [
        ("GET",  "/items",         "List all inventory items (id, name)"),
        ("POST", "/items",         "Create a new item (body: name, quantity)"),
        ("GET",  "/items/{id}",    "Get full details of an item"),
    ];

    /// GET — returns HTML list of all business endpoints
    [HttpGet]
    [Produces("text/html")]
    public IActionResult Index()
    {
        var rows = Endpoints.Select(e => new[] { e.Method, e.Path, e.Description });
        var table = ContentNegotiator.Table(["Method", "Path", "Description"], rows);
        var html = ContentNegotiator.Page("Simple Inventory — API Endpoints", table);
        return Content(html, "text/html; charset=utf-8");
    }
}