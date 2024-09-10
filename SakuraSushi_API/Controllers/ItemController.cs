using Microsoft.AspNetCore.Mvc;
using SakuraSushi_API.DataContext;

namespace SakuraSushi_API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ItemController : Controller
    {
        private SakuraSushiContext _context;

        public ItemController(SakuraSushiContext context) {
            _context = context;
        }
        

        [HttpGet("/api/Items")]
        public IActionResult Index([FromQuery] string? search)
        {

            var items = _context.Items.ToList().Select(s => s);
            var categories = _context.Categories.OrderBy(s => s.Name).ToList().Select(s => new {
                name = s.Name,
                description = s.Description,
                items = items.Where(j => j.CategoryId == s.Id).Select(k => new
                {
                    name = k.Name,
                    id = k.Id,
                    description = k.Description,
                    price = k.Price,
                    available = k.Available
                }).ToList()
            });

            if (search != null)
            {
                items = _context.Items.Where(s => s.Name.Contains(search)).ToList().Select(s => s);
                categories = _context.Categories.OrderBy(s => s.Name).ToList().Select(s => new {
                    name = s.Name,
                    description = s.Description,
                    items = items.Where(j => j.CategoryId == s.Id).Select(k => new
                    {
                        name = k.Name,
                        id = k.Id,
                        description = k.Description,
                        price = k.Price,
                        available = k.Available
                    }).ToList()
                }).Where(s => s.items.Count != 0).ToList();
            }

            return Ok(categories);
        }
    }
}
