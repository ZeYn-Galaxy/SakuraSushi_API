using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SakuraSushi_API.DataContext;
using SakuraSushi_API.Request;

namespace SakuraSushi_API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TransactionController : Controller
    {
        private SakuraSushiContext _context;

        public TransactionController(SakuraSushiContext context)
        {
            _context = context;
        }


        [Authorize]
        [HttpPost("/api/Transaction")]
        public IActionResult Index([FromForm] string tableNumber)
        {
            var user = User.Claims.FirstOrDefault(s => s.Type == "user_id");
            if (user == null)
            {
                return Unauthorized();
            }

            var userId = Guid.Parse(user.Value);
            var userData = _context.Users.FirstOrDefault(s => s.Id == userId);
            if (userData == null)
            {
                return NotFound("User not found");
            }

            if (userData.Role != "Cashier" || userData.Role != "Waiter")
            {
                return BadRequest("You need permission");
            }


            var exist = _context.Tables.FirstOrDefault(s => s.TableNumber == tableNumber);

            if (exist == null)
            {
                return NotFound("Table not found");
            }

            var transaction = _context.Transactions.FirstOrDefault(s => s.TableId == exist.Id);

            if (transaction != null)
            {
                return BadRequest("Table already has an open transaction");
            }

            var code = generate_unique_code();
            var newTransaction = new Transaction
            {
                Id = Guid.NewGuid(),
                TableId = exist.Id,
                CashierId = userId,
                OpenedAt = DateTime.Now,
                TotalAmount = 0,
                UniqueCode = code
            };

            _context.Transactions.Add(newTransaction);
            _context.SaveChanges();


            return Ok(code);
        }
        
        [Authorize]
        [HttpGet("/api/Transaction/{uniqueCode}/Orders")]
        public IActionResult getOrders([FromRoute] string uniqueCode)
        {
            var user = User.Claims.FirstOrDefault(s => s.Type == "user_id");
            if ( user == null )
            {
                return Unauthorized();
            }

            var userId = Guid.Parse(user.Value);

            var transaction = _context.Transactions.FirstOrDefault(s => s.UniqueCode == uniqueCode && s.CashierId == userId);

            if (transaction == null)
            {
                return NotFound("Transaction not found");
            }

            if (transaction.ClosedAt != null)
            {
                return BadRequest("Transacation has been closed");
            }

            var orders = _context.Orders.Where(s => s.TransactionId == transaction.Id).Select(s => new
            {
                id = s.Id,
                orderedAt = s.OrderedAt,
                totalAmount = s.Amount,
                orderItems = s.OrderItems.Select(k => new
                {
                    id = k.Id,
                    quantity = k.Quantity,
                    price = k.Price,
                    status = k.Status,
                    item = new
                    {
                        name = k.Item.Name,
                        id = k.Item.Id,
                        description = k.Item.Description,
                        price = k.Item.Price,
                        available = k.Item.Available
                    }
                }).ToList()
            });

            return Ok(orders);

        }

        [Authorize]
        [HttpPost("/api/Transaction/{uniqueCode}/Pay")]
        public IActionResult Pay([FromRoute] string uniqueCode) {
            var user = User.Claims.FirstOrDefault(s => s.Type == "user_id");
            if (user == null)
            {
                return Unauthorized();
            }

            var userId = Guid.Parse(user.Value);
            var userData = _context.Users.FirstOrDefault(s => s.Id == userId);
            if (userData == null)
            {
                return NotFound("User not found");
            }

            if (userData.Role != "Cashier")
            {
                return BadRequest("You don't have a permission");
            }

            var transaction = _context.Transactions.FirstOrDefault(s => s.UniqueCode == uniqueCode && s.CashierId == userId);

            if (transaction == null)
            {
                return NotFound("Transaction not found");
            }

            if (transaction.ClosedAt != null)
            {
                return BadRequest("Transaction is already paid");
            }

            transaction.ClosedAt = DateTime.Now;
            _context.SaveChanges();

            return Ok(new
            {
                uniqueCode = uniqueCode,
                openedAt = transaction.OpenedAt,
                closedAt = transaction.ClosedAt,
                totalAmount = transaction.TotalAmount,
                orders = _context.Orders.Where(s => s.TransactionId == transaction.Id).Select(s => new
                {
                    id = s.Id,
                    amount = s.Amount,
                    orderedAt = s.OrderedAt,
                    orderItems = s.OrderItems.Select(k => new
                    {
                        id = k.Id,
                        quantity = k.Quantity,
                        price = k.Price,
                        status = k.Status,
                        item = new
                        {
                            id = k.Item.Id,
                            name = k.Item.Name,
                            description = k.Item.Description,
                            price = k.Item.Price,
                            available = k.Item.Available
                        }
                    }).ToList()
                }).ToList()
            });

        }

        [Authorize]
        [HttpPut("/api/Transaction/{uniqueCode}/Orders/{orderId}/Items/{itemId}/Status")]
        public IActionResult updateStatus([FromRoute] string uniqueCode, [FromRoute] string orderId, [FromRoute] string itemId, [FromForm] [Required] string status)
        {
            var user = User.Claims.FirstOrDefault(s => s.Type == "user_id");
            if (user == null)
            {
                return Unauthorized();
            }

            var userId = Guid.Parse(user.Value);
            var userData = _context.Users.FirstOrDefault(s => s.Id == userId);
            if (userData == null)
            {
                return NotFound("User not found");
            }

            if (userData.Role != "Chef" || userData.Role != "Waiter")
            {
                return BadRequest("You don't have a permission");
            }
            var transaction = _context.Transactions.FirstOrDefault(s => s.UniqueCode == uniqueCode && s.CashierId == userId);

            if (transaction == null)
            {
                return NotFound("Transaction not found");
            }

            if (transaction.ClosedAt != null)
            {
                return BadRequest("Transaction has been closed");
            }

            var order_id = Guid.Parse(orderId);
            var order = _context.Orders.FirstOrDefault(s => s.Id == order_id);

            if (order == null)
            {
                return NotFound("Order not found");
            }

            var item_id = Guid.Parse(itemId);
            var item = _context.Items.FirstOrDefault(s => s.Id == item_id);

            if (item == null)
            {
                return NotFound("Item not found");
            }

            var orderItem = _context.OrderItems.FirstOrDefault(s => s.OrderId == order.Id && s.ItemId == item_id);

            if (orderItem == null)
            {
                return NotFound("Order item not found");
            }

            orderItem.Status = status;
            _context.SaveChanges();

            return Ok(new
            {
                quantity = orderItem.Quantity,
                price = orderItem.Price,
                status = orderItem.Status,
                item = new
                {
                    id = orderItem.Item.Id,
                    name = orderItem.Item.Name,
                    description = orderItem.Item.Description,
                    price = orderItem.Item.Price,
                    available = orderItem.Item.Available
                }
            });
            
        }

        private string generate_unique_code()
        {
            var transaction = _context.Transactions.ToList();

            string check()
            {
                var kode = Guid.NewGuid().ToString("N").Substring(0, 4).ToUpper();

                if (transaction.Any(s => s.UniqueCode == kode))
                {
                    return check();
                }

                return kode;
            }


            return check();
        }
    }
}
