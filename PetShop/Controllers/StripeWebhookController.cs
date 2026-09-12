using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetShop.DataContext;
using PetShop.DataContext.Entites;
using Stripe;

namespace PetShop.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Route("webhook/stripe")]
    [IgnoreAntiforgeryToken]
    public class StripeWebhookController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<StripeWebhookController> _logger;

        public StripeWebhookController(
            AppDbContext context, 
            IConfiguration configuration, 
            ILogger<StripeWebhookController> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> Index()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            var webhookSecret = _configuration["Stripe:WebhookSecret"];
            string? signatureHeader = Request.Headers["Stripe-Signature"];

            Event stripeEvent;

            try
            {
                if (!string.IsNullOrEmpty(webhookSecret) && !string.IsNullOrEmpty(signatureHeader))
                {
                    stripeEvent = EventUtility.ConstructEvent(json, signatureHeader, webhookSecret, throwOnApiVersionMismatch: false);
                }
                else
                {
                    try
                    {
                        stripeEvent = EventUtility.ParseEvent(json, throwOnApiVersionMismatch: false);
                    }
                    catch
                    {
                        stripeEvent = Newtonsoft.Json.JsonConvert.DeserializeObject<Event>(json)!;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Stripe webhook konstruksiyasında xəta baş verdi.");
                return BadRequest(new { error = ex.Message });
            }

            if (stripeEvent == null)
            {
                return BadRequest(new { error = "Invalid event payload" });
            }

            _logger.LogInformation("Stripe webhook hadisəsi qəbul edildi: {EventType}", stripeEvent.Type);

            if (stripeEvent.Type == "payment_intent.succeeded")
            {
                var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                if (paymentIntent != null)
                {
                    await HandlePaymentIntentSucceededAsync(paymentIntent);
                }
            }
            else if (stripeEvent.Type == "payment_intent.payment_failed")
            {
                var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                if (paymentIntent != null)
                {
                    await HandlePaymentIntentFailedAsync(paymentIntent);
                }
            }

            return Ok(new { status = "success", eventType = stripeEvent.Type });
        }

        private async Task HandlePaymentIntentSucceededAsync(PaymentIntent paymentIntent)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.PaymentIntentId == paymentIntent.Id);

            if (order == null && paymentIntent.Metadata != null)
            {
                if (paymentIntent.Metadata.TryGetValue("OrderNumber", out var orderNumber))
                {
                    order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);
                }
                else if (paymentIntent.Metadata.TryGetValue("OrderId", out var orderIdStr) && int.TryParse(orderIdStr, out var orderId))
                {
                    order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
                }
            }

            if (order != null)
            {
                order.Status = OrderStatus.Paid;
                if (string.IsNullOrEmpty(order.PaymentIntentId))
                {
                    order.PaymentIntentId = paymentIntent.Id;
                }
                order.PaymentMethod = "Stripe";
                await _context.SaveChangesAsync();
                _logger.LogInformation("#{OrderNumber} nömrəli sifariş Stripe webhook vasitəsilə 'Paid' olaraq yeniləndi.", order.OrderNumber);
            }
            else
            {
                _logger.LogWarning("PaymentIntent ID {PaymentIntentId} üçün bazada uyğun sifariş tapılmadı.", paymentIntent.Id);
            }
        }

        private async Task HandlePaymentIntentFailedAsync(PaymentIntent paymentIntent)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.PaymentIntentId == paymentIntent.Id);
            if (order != null)
            {
                _logger.LogWarning("#{OrderNumber} nömrəli sifariş üçün Stripe ödənişi uğursuz oldu.", order.OrderNumber);
            }
        }
    }
}
