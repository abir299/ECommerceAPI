using ECommerceAPI.Data;
using ECommerceAPI.Data.Entities;
using ECommerceAPI.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ECommerceAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController(ECommerceDbContext context) : ControllerBase
    {
        private int? GetCurrentUserId()
        {
            var claim = User.FindFirst("UserId")?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            return int.TryParse(claim, out int userId) ? userId : null;
        }

        private static OrderResponseDto MapOrder(Order order)
        {
            return new OrderResponseDto
            {
                Id = order.Id,
                UserId = order.UserId,
                ShippingAddressId = order.ShippingAddressId,
                TotalAmount = order.TotalAmount,
                Status = order.Status,
                OrderDate = order.OrderDate,

                OrderItems = order.OrderItems.Select(i =>
                    new OrderItemResponseDto
                    {
                        Id = i.Id,
                        ProductId = i.ProductId,
                        ProductName = i.Product.Name,
                        ProductVariantId = i.ProductVariantId,
                        Size = i.ProductVariant?.Size,
                        Color = i.ProductVariant?.Color,
                        Quantity = i.Quantity,
                        UnitPrice = i.UnitPrice
                    }).ToList(),

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
        public async Task<IActionResult> Checkout()
        {
            var userId = GetCurrentUserId();

            if (userId == null)
                return Unauthorized(new { message = "Invalid user token." });

            var address = await context.Addresses
                .FirstOrDefaultAsync(a =>
                    a.UserId == userId.Value && a.IsDefault)
                ?? await context.Addresses
                    .FirstOrDefaultAsync(a => a.UserId == userId.Value);

            if (address == null)
                return BadRequest(new
                {
                    message = "Please add a shipping address before checkout."
                });

            var cart = await context.Carts
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.ProductVariant)
                .FirstOrDefaultAsync(c => c.UserId == userId.Value);

            if (cart == null || !cart.CartItems.Any())
                return BadRequest(new { message = "Your cart is empty." });

            await using var transaction =
                await context.Database.BeginTransactionAsync();

            try
            {
                var order = new Order
                {
                    UserId = userId.Value,
                    ShippingAddressId = address.Id,
                    Status = "Pending",
                    TotalAmount = 0,
                    OrderDate = DateTime.UtcNow
                };

                context.Orders.Add(order);
                await context.SaveChangesAsync();

                decimal totalAmount = 0;

                foreach (var item in cart.CartItems)
                {
                    if (item.Quantity <= 0)
                    {
                        await transaction.RollbackAsync();
                        return BadRequest(new
                        {
                            message = "Quantity must be greater than zero."
                        });
                    }

                    if (item.ProductVariantId == null)
                    {
                        await transaction.RollbackAsync();
                        return BadRequest(new
                        {
                            message = $"Select a product variant for product {item.ProductId}."
                        });
                    }

                    var variant = await context.ProductVariants
                        .FirstOrDefaultAsync(v =>
                            v.Id == item.ProductVariantId.Value &&
                            v.ProductId == item.ProductId);

                    if (variant == null)
                    {
                        await transaction.RollbackAsync();
                        return BadRequest(new
                        {
                            message = $"Variant not found for product {item.ProductId}."
                        });
                    }

                    var affectedRows = await context.ProductVariants
                        .Where(v =>
                            v.Id == variant.Id &&
                            v.StockQuantity >= item.Quantity)
                        .ExecuteUpdateAsync(setters => setters
                            .SetProperty(
                                v => v.StockQuantity,
                                v => v.StockQuantity - item.Quantity));

                    if (affectedRows == 0)
                    {
                        await transaction.RollbackAsync();
                        return BadRequest(new
                        {
                            message = $"Insufficient stock for variant {variant.Id}."
                        });
                    }

                    decimal unitPrice =
                        item.Product.Price + variant.AdditionalPrice;

                    totalAmount += unitPrice * item.Quantity;

                    context.OrderItems.Add(new OrderItem
                    {
                        OrderId = order.Id,
                        ProductId = item.ProductId,
                        ProductVariantId = variant.Id,
                        Quantity = item.Quantity,
                        UnitPrice = unitPrice
                    });
                }

                order.TotalAmount = totalAmount;

                context.OrderStatusHistories.Add(new OrderStatusHistory
                {
                    OrderId = order.Id,
                    Status = "Pending",
                    ChangedAt = DateTime.UtcNow,
                    Note = "Order placed."
                });

                context.CartItems.RemoveRange(cart.CartItems);

                await context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new
                {
                    message = "Checkout successful.",
                    orderId = order.Id,
                    totalAmount = order.TotalAmount,
                    status = order.Status
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
            var userId = GetCurrentUserId();

            if (userId == null)
                return Unauthorized();

            var orders = await context.Orders
                .Where(o => o.UserId == userId.Value)
                .Include(o => o.OrderItems)
                    .ThenInclude(i => i.Product)
                .Include(o => o.OrderItems)
                    .ThenInclude(i => i.ProductVariant)
                .Include(o => o.Payment)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return Ok(orders.Select(MapOrder).ToList());
        }

        [HttpGet("{id:int}")]
        [Authorize(Roles = "Customer,Admin")]
        public async Task<IActionResult> GetOrder(int id)
        {
            var order = await context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(i => i.Product)
                .Include(o => o.OrderItems)
                    .ThenInclude(i => i.ProductVariant)
                .Include(o => o.Payment)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return NotFound(new { message = "Order not found." });

            if (User.IsInRole("Customer"))
            {
                var userId = GetCurrentUserId();

                if (userId == null)
                    return Unauthorized();

                if (order.UserId != userId.Value)
                    return Forbid();
            }

            return Ok(MapOrder(order));
        }

        [HttpPut("{id:int}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateStatus(
            int id, [FromBody] string status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return BadRequest(new { message = "Status is required." });

            status = status.Trim();

            var order = await context.Orders
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return NotFound(new { message = "Order not found." });

            var allowedTransitions =
                new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Pending"] = new[] { "Confirmed", "Cancelled" },
                    ["Confirmed"] = new[] { "Processing", "Cancelled" },
                    ["Processing"] = new[] { "Shipped", "Cancelled" },
                    ["Shipped"] = new[] { "Delivered" },
                    ["Delivered"] = Array.Empty<string>(),
                    ["Cancelled"] = Array.Empty<string>()
                };

            if (!allowedTransitions.TryGetValue(
                    order.Status, out var nextStatuses))
            {
                return BadRequest(new
                {
                    message = $"Unknown current order status: {order.Status}."
                });
            }

            if (!nextStatuses.Contains(
                    status, StringComparer.OrdinalIgnoreCase))
            {
                return BadRequest(new
                {
                    message =
                        $"Cannot change order status from {order.Status} to {status}.",
                    currentStatus = order.Status,
                    allowedNextStatuses = nextStatuses
                });
            }

            status = nextStatuses.First(s =>
                s.Equals(status, StringComparison.OrdinalIgnoreCase));

            var previousStatus = order.Status;
            order.Status = status;

            context.OrderStatusHistories.Add(new OrderStatusHistory
            {
                OrderId = order.Id,
                Status = status,
                ChangedAt = DateTime.UtcNow,
                Note = $"Status changed from {previousStatus} to {status}."
            });

            await context.SaveChangesAsync();

            return Ok(new
            {
                message = "Order status updated and history saved.",
                orderId = id,
                previousStatus,
                status
            });
        }

        [HttpGet("{id:int}/status-history")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetStatusHistory(int id)
        {
            var orderExists = await context.Orders
                .AnyAsync(o => o.Id == id);

            if (!orderExists)
                return NotFound(new { message = "Order not found." });

            var history = await context.OrderStatusHistories
                .Where(h => h.OrderId == id)
                .OrderBy(h => h.ChangedAt)
                .Select(h => new
                {
                    h.Id,
                    h.OrderId,
                    h.Status,
                    h.ChangedAt,
                    h.Note
                })
                .ToListAsync();

            return Ok(history);
        }

        [HttpPut("{id:int}/cancel")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> CancelOrder(int id)
        {
            var userId = GetCurrentUserId();

            if (userId == null)
                return Unauthorized(new { message = "Invalid user token." });

            await using var transaction =
                await context.Database.BeginTransactionAsync();

            try
            {
                var order = await context.Orders
                    .AsNoTracking()
                    .FirstOrDefaultAsync(o =>
                        o.Id == id && o.UserId == userId.Value);

                if (order == null)
                {
                    await transaction.RollbackAsync();
                    return NotFound(new { message = "Order not found." });
                }

                if (order.Status != "Pending" &&
                    order.Status != "Confirmed")
                {
                    await transaction.RollbackAsync();
                    return BadRequest(new
                    {
                        message = $"Order cannot be cancelled when status is {order.Status}."
                    });
                }

                var updated = await context.Orders
                    .Where(o =>
                        o.Id == id &&
                        o.UserId == userId.Value &&
                        (o.Status == "Pending" || o.Status == "Confirmed"))
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(o => o.Status, "Cancelled"));

                if (updated == 0)
                {
                    await transaction.RollbackAsync();
                    return Conflict(new
                    {
                        message = "Order status changed. Please refresh and try again."
                    });
                }

                var items = await context.OrderItems
                    .AsNoTracking()
                    .Where(i => i.OrderId == id)
                    .ToListAsync();

                foreach (var item in items)
                {
                    if (item.ProductVariantId.HasValue)
                    {
                        await context.ProductVariants
                            .Where(v => v.Id == item.ProductVariantId.Value)
                            .ExecuteUpdateAsync(setters => setters
                                .SetProperty(
                                    v => v.StockQuantity,
                                    v => v.StockQuantity + item.Quantity));
                    }
                }

                context.OrderStatusHistories.Add(new OrderStatusHistory
                {
                    OrderId = id,
                    Status = "Cancelled",
                    ChangedAt = DateTime.UtcNow,
                    Note = $"Order cancelled by customer. Previous status: {order.Status}. Stock restored."
                });

                await context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new
                {
                    message = "Order cancelled and stock restored successfully.",
                    orderId = id,
                    previousStatus = order.Status,
                    status = "Cancelled"
                });
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}