
using System.Security.Claims;
using ECommerceAPI.Data;
using ECommerceAPI.Data.Entities;
using ECommerceAPI.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerceAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OrdersController : ControllerBase
    {
        private readonly ECommerceDbContext _context;

        public OrdersController(ECommerceDbContext context)
        {
            _context = context;
        }

        private int GetUserId()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                         ?? User.FindFirstValue("sub");

            return int.TryParse(userId, out var id) ? id : 0;
        }

        private async Task<OrderResponseDto> MapOrder(Order order)
        {
            var items = new List<OrderItemResponseDto>();

            foreach (var item in order.OrderItems)
            {
                var productName = await _context.Products
                    .Where(p => p.Id == item.ProductId)
                    .Select(p => p.Name)
                    .FirstOrDefaultAsync();

                items.Add(new OrderItemResponseDto
                {
                    Id = item.Id,
                    ProductId = item.ProductId,
                    ProductName = productName ?? "Unknown Product",
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice
                });
            }

            return new OrderResponseDto
            {
                Id = order.Id,
                UserId = order.UserId,
                ShippingAddressId = order.ShippingAddressId,
                TotalAmount = order.TotalAmount,
                Status = order.Status,
                OrderDate = order.OrderDate,
                OrderItems = items,
                Payment = order.Payment == null ? null : new PaymentResponseDto
                {
                    Id = order.Payment.Id,
                    Amount = order.Payment.Amount,
                    PaymentMethod = order.Payment.PaymentMethod,
                    Status = order.Payment.Status,
                    TransactionId = order.Payment.TransactionId,
                    PaymentDate = order.Payment.PaymentDate
                }
            };
        }

        [HttpPost("checkout")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Checkout(CreateOrderDto dto)
        {
            var userId = GetUserId();

            if (userId <= 0)
                return Unauthorized();

            if (dto.PaymentMethod != "CashOnDelivery")
                return BadRequest("Only CashOnDelivery is supported currently.");

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                var address = await _context.Addresses
                    .FirstOrDefaultAsync(a =>
                        a.Id == dto.ShippingAddressId &&
                        a.UserId == userId);

                if (address == null)
                    return BadRequest("Invalid shipping address.");

                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (cart == null || !cart.CartItems.Any())
                    return BadRequest("Your cart is empty.");

                var orderItems = new List<OrderItem>();
                decimal totalAmount = 0;

                foreach (var item in cart.CartItems)
                {
                    var product = await _context.Products
                        .FirstOrDefaultAsync(p => p.Id == item.ProductId);

                    if (product == null)
                        return BadRequest(
                            $"Product {item.ProductId} was not found.");

                    var stock = await _context.Stocks
                        .FirstOrDefaultAsync(s => s.ProductId == item.ProductId);

                    if (stock == null ||
                        stock.Quantity - stock.ReservedQuantity < item.Quantity)
                    {
                        return BadRequest(
                            $"Insufficient stock for product {product.Name}.");
                    }

                    stock.Quantity -= item.Quantity;
                    stock.UpdatedAt = DateTime.UtcNow;

                    totalAmount += product.Price * item.Quantity;

                    orderItems.Add(new OrderItem
                    {
                        ProductId = product.Id,
                        Quantity = item.Quantity,
                        UnitPrice = product.Price
                    });
                }

                var order = new Order
                {
                    UserId = userId,
                    ShippingAddressId = dto.ShippingAddressId,
                    TotalAmount = totalAmount,
                    Status = "Pending",
                    OrderDate = DateTime.UtcNow,
                    OrderItems = orderItems,
                    Payment = new Payment
                    {
                        Amount = totalAmount,
                        PaymentMethod = "CashOnDelivery",
                        Status = "Pending",
                        PaymentDate = DateTime.UtcNow
                    }
                };

                _context.Orders.Add(order);
                _context.CartItems.RemoveRange(cart.CartItems);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new
                {
                    message = "Order placed successfully.",
                    orderId = order.Id,
                    totalAmount = order.TotalAmount,
                    status = order.Status,
                    paymentMethod = "CashOnDelivery"
                });
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        [HttpGet("my-orders")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> GetMyOrders()
        {
            var userId = GetUserId();

            if (userId <= 0)
                return Unauthorized();

            var orders = await _context.Orders
                .Where(o => o.UserId == userId)
                .Include(o => o.OrderItems)
                .Include(o => o.Payment)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            var result = new List<OrderResponseDto>();

            foreach (var order in orders)
                result.Add(await MapOrder(order));

            return Ok(result);
        }

        [HttpGet("{id:int}")]
        [Authorize(Roles = "Customer,Admin")]
        public async Task<IActionResult> GetOrder(int id)
        {
            var userId = GetUserId();

            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .Include(o => o.Payment)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return NotFound("Order not found.");

            if (!User.IsInRole("Admin") && order.UserId != userId)
                return Forbid();

            return Ok(await MapOrder(order));
        }

        [HttpPut("{id:int}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateStatus(
            int id, [FromBody] string status)
        {
            var allowedStatuses = new[]
            {
                "Pending", "Confirmed", "Processing",
                "Shipped", "Delivered", "Cancelled"
            };

            if (string.IsNullOrWhiteSpace(status) ||
                !allowedStatuses.Contains(status,
                    StringComparer.OrdinalIgnoreCase))
            {
                return BadRequest("Invalid order status.");
            }

            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return NotFound("Order not found.");

            order.Status = allowedStatuses.First(
                s => s.Equals(status, StringComparison.OrdinalIgnoreCase));

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Order status updated.",
                orderId = order.Id,
                status = order.Status
            });
        }
    }
}
