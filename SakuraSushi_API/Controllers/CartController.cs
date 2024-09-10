using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SakuraSushi_API.DataContext;
using SakuraSushi_API.Request;

namespace SakuraSushi_API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CartController : Controller
    {
        private SakuraSushiContext _context;

        public CartController(SakuraSushiContext context)
        {
            _context = context;
        }


        [Authorize]
        [HttpGet("/api/Transaction/{uniqueCode}/Cart")]
        public IActionResult getCart([FromRoute] string uniqueCode)
        {
            var user = User.Claims.FirstOrDefault(s => s.Type == "user_id");
            if (user == null)
            {
                return Unauthorized();
            }

            var id = Guid.Parse(user.Value);
            var transaction = _context.Transactions.FirstOrDefault(s => s.CashierId == id && s.UniqueCode == uniqueCode);
            if (transaction == null)
            {
                return NotFound("Transaction not found");
            }

            var data = _context.CartItems.Where(s => s.TransactionId == transaction.Id).Select(s => new
            {
                quantity = s.Quantity,
                totalPrice = s.TotalPrice,
                addedAt = s.AddedAt,
                item = new
                {
                    name = s.Item.Name,
                    id = s.Item.Id,
                    description = s.Item.Description,
                    price = s.Item.Price,
                    available = s.Item.Available
                }
            }).ToList();

            return Ok(data);

        }

        [Authorize]
        [HttpPost("/api/Transaction/{uniqueCode}/Cart")]
        public IActionResult addCart([FromRoute] string uniqueCode, [FromBody] CartRequire req)
        {
            var user = User.Claims.FirstOrDefault(s => s.Type == "user_id");
            if (user == null)
            {
                return Unauthorized();
            }

            var userId = Guid.Parse(user.Value);
            var transaction = _context.Transactions.FirstOrDefault(s => s.CashierId == userId && s.UniqueCode == uniqueCode);
            if (transaction == null)
            {
                return NotFound("Transaction not found");
            }

            if (transaction.ClosedAt != null)
            {
                return BadRequest("Transaction has been closed");
            }

            var id = Guid.Parse(req.itemId);
            var item = _context.Items.FirstOrDefault(s => s.Id == id);
            if (item == null)
            {
                return NotFound("Item not found");
            }

            var cartItem = _context.CartItems.FirstOrDefault(s => s.TransactionId == transaction.Id && s.ItemId == item.Id);
            if (cartItem != null)
            {
                cartItem.Quantity += req.quantity;
                cartItem.TotalPrice = (cartItem.Quantity + req.quantity) * cartItem.Price;
                _context.SaveChanges();
            }
            else
            {
                cartItem = new CartItem
                {
                    Id = Guid.NewGuid(),
                    TransactionId = transaction.Id,
                    ItemId = id,
                    Quantity = req.quantity,
                    Price = item.Price,
                    TotalPrice = req.quantity * item.Price,
                    AddedAt = DateTime.Now
                };

                _context.CartItems.Add(cartItem);
                _context.SaveChanges();
            }


            return Ok(new
            {
                quantity = cartItem.Quantity,
                totalPrice = cartItem.TotalPrice,
                addedAt = cartItem.AddedAt,
                item = new
                {
                    name = item.Name,
                    id = item.Id,
                    description = item.Description,
                    price = item.Price,
                    available = item.Available
                }
            });
        }

        [Authorize]
        [HttpDelete("/api/Transaction/{uniqueCode}/Cart/{itemId}")]
        public IActionResult deleteCart([FromRoute] string uniqueCode, [FromRoute] string itemId)
        {
            var user = User.Claims.FirstOrDefault(s => s.Type == "user_id");
            if (user == null)
            {
                return Unauthorized();
            }
            var id = Guid.Parse(user.Value);
            var item = Guid.Parse(itemId);
            var itemchecked = _context.Items.FirstOrDefault(s => s.Id == item);
            if (itemchecked == null)
            {
                return NotFound("Item not found");
            }
            var transaction = _context.Transactions.FirstOrDefault(s => s.UniqueCode == uniqueCode && s.CashierId == id);
            if (transaction == null)
            {
                return BadRequest("Transactio not found");
            }
            if (transaction.ClosedAt != null)
            {
                return BadRequest("Transacation has been closed");
            }

            var cartItem = _context.CartItems.FirstOrDefault(s => s.TransactionId == transaction.Id && s.ItemId == item);
            if (cartItem == null)
            {
                return NotFound("Item not found in cart");
            }

            _context.CartItems.Remove(cartItem);
            _context.SaveChanges();

            return NoContent();
        }

        [Authorize]
        [HttpPost("/api/Transaction/{uniqueCode}/Cart/Order")]
        public IActionResult addToOrder([FromRoute] string uniqueCode)
        {
            var user = User.Claims.FirstOrDefault(s => s.Type == "user_id");
            if (user == null)
            {
                return Unauthorized();
            }
            var userId = Guid.Parse(user.Value);
            var transaction = _context.Transactions.FirstOrDefault(s => s.UniqueCode == uniqueCode && s.CashierId == userId);
            if (transaction == null)
            {
                return BadRequest("Transaction not found");
            }
            if (transaction.ClosedAt != null)
            {
                return BadRequest("Transacation has been closed");
            }

            var newOrder = new Order
            {
                Id = Guid.NewGuid(),
                TransactionId = transaction.Id,
                OrderedAt = DateTime.Now,
                Amount = 0
            };

            _context.Orders.Add(newOrder);

            var cartItems = _context.CartItems.Where(s => s.TransactionId == transaction.Id).ToList();
            if (cartItems.Count == 0)
            {
                return BadRequest("The cart is empty");
            }

            foreach(var item in cartItems)
            {
                var exist = _context.Items.FirstOrDefault(s => s.Id == item.ItemId);
                if (exist == null)
                {
                    return NotFound("Item not found");
                }

                var newOrderItems = new OrderItem
                {
                    Id = Guid.NewGuid(),
                    OrderId = newOrder.Id,
                    ItemId = item.ItemId,
                    Quantity = item.Quantity,
                    Price = item.Price,
                    Status = "InProgress"
                };

                newOrder.Amount += newOrderItems.Quantity;
                _context.CartItems.Remove(item);
                _context.OrderItems.Add(newOrderItems);
            }

            transaction.TotalAmount += newOrder.Amount;
            _context.SaveChanges();

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

            return Created(uniqueCode, orders);

        }

    }
}
