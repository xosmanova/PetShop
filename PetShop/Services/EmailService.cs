using System.Text;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using PetShop.DataContext.Entites;

namespace PetShop.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlMessage)
        {
            var host = _configuration["Smtp:Server"] ?? "sandbox.smtp.mailtrap.io";
            var port = int.TryParse(_configuration["Smtp:Port"], out var p) ? p : 2525;
            var senderName = _configuration["Smtp:SenderName"] ?? "PetShop Azerbaijan";
            var senderEmail = _configuration["Smtp:SenderEmail"] ?? "no-reply@petshop.az";
            var username = _configuration["Smtp:Username"] ?? "";
            var password = _configuration["Smtp:Password"] ?? "";
            var enableSsl = bool.TryParse(_configuration["Smtp:EnableSsl"], out var ssl) && ssl;
            var logToConsole = !bool.TryParse(_configuration["Smtp:LogToConsole"], out var ltc) || ltc;

            if (logToConsole)
            {
                _logger.LogInformation("\n==================================================");
                _logger.LogInformation(">>> [EMAIL SERVICE DISPATCH]");
                _logger.LogInformation("To: {ToEmail}", toEmail);
                _logger.LogInformation("Subject: {Subject}", subject);
                _logger.LogInformation("Host: {Host}:{Port} | Sender: {SenderName} <{SenderEmail}>", host, port, senderName, senderEmail);
                _logger.LogInformation("==================================================\n");
            }

            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(senderName, senderEmail));
                message.To.Add(MailboxAddress.Parse(toEmail));
                message.Subject = subject;

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = htmlMessage
                };
                message.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();
                client.ServerCertificateValidationCallback = (s, c, h, e) => true;

                var socketOptions = enableSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTlsWhenAvailable;

                await client.ConnectAsync(host, port, socketOptions);

                // Əgər real Mailtrap/SMTP hesabı qeyd olunubsa göndər, placeholder olduqda simulyasiya et
                if (!string.IsNullOrWhiteSpace(username) && 
                    !username.Contains("test_user") && 
                    !username.Contains("placeholder") && 
                    !string.IsNullOrWhiteSpace(password) && 
                    !password.Contains("test_password"))
                {
                    await client.AuthenticateAsync(username, password);
                    await client.SendAsync(message);
                    _logger.LogInformation("Email uğurla SMTP vasitəsilə göndərildi: {ToEmail}", toEmail);
                }
                else
                {
                    _logger.LogInformation("SMTP tənzimləmələri test/development rejimindədir. Email simulyasiya olundu və qeydə alındı ({ToEmail}).", toEmail);
                }

                await client.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "SMTP serverinə qoşulma zamanı xəbərdarlıq (Development/Test mühiti). Email loqa yazıldı, müştəri əməliyyatı davam etdirildi.");
            }
        }

        public async Task SendOrderConfirmationEmailAsync(Order order)
        {
            var sb = new StringBuilder();

            var paymentMethodText = order.PaymentMethod switch
            {
                "Stripe" => "Bank Kartı (Stripe - Ödənilib)",
                "CashOnDelivery" => "Nağd (Çatdırılmada)",
                _ => order.PaymentMethod ?? "Nağd"
            };

            var statusBadge = order.Status switch
            {
                OrderStatus.Paid => "<span style='background-color:#198754;color:#fff;padding:4px 10px;border-radius:12px;font-size:12px;font-weight:600;'>Ödənilib</span>",
                OrderStatus.Processing => "<span style='background-color:#0d6efd;color:#fff;padding:4px 10px;border-radius:12px;font-size:12px;font-weight:600;'>Hazırlanır</span>",
                OrderStatus.Shipped => "<span style='background-color:#0dcaf0;color:#000;padding:4px 10px;border-radius:12px;font-size:12px;font-weight:600;'>Çatdırılmada</span>",
                OrderStatus.Delivered => "<span style='background-color:#198754;color:#fff;padding:4px 10px;border-radius:12px;font-size:12px;font-weight:600;'>Təhvil Verildi</span>",
                OrderStatus.Cancelled => "<span style='background-color:#dc3545;color:#fff;padding:4px 10px;border-radius:12px;font-size:12px;font-weight:600;'>Ləğv Edildi</span>",
                _ => "<span style='background-color:#ffc107;color:#000;padding:4px 10px;border-radius:12px;font-size:12px;font-weight:600;'>Gözləmədə</span>"
            };

            sb.Append($@"
<!DOCTYPE html>
<html lang='az'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Sifariş Təsdiqi</title>
</head>
<body style='margin:0;padding:0;background-color:#f4f6f9;font-family:Segoe UI,Tahoma,Geneva,Verdana,sans-serif;color:#333333;'>
    <table border='0' cellpadding='0' cellspacing='0' width='100%' style='background-color:#f4f6f9;padding:25px 15px;'>
        <tr>
            <td align='center'>
                <table border='0' cellpadding='0' cellspacing='0' width='620' style='background-color:#ffffff;border-radius:12px;overflow:hidden;box-shadow:0 6px 20px rgba(0,0,0,0.06);'>
                    <!-- Header -->
                    <tr>
                        <td style='background:linear-gradient(135deg, #198754, #20c997);padding:35px 30px;text-align:center;color:#ffffff;'>
                            <div style='font-size:32px;font-weight:800;letter-spacing:1px;'>🐾 PetShop Azerbaijan</div>
                            <div style='font-size:16px;margin-top:8px;opacity:0.92;'>Sifarişiniz Uğurla Qəbul Olundu!</div>
                        </td>
                    </tr>

                    <!-- Body Content -->
                    <tr>
                        <td style='padding:30px;'>
                            <h2 style='margin-top:0;font-size:20px;color:#212529;'>Hörmətli {order.CustomerName},</h2>
                            <p style='color:#555;font-size:15px;line-height:1.6;'>
                                PetShop-u seçdiyiniz üçün təşəkkür edirik! Sifarişiniz sistemimiz tərəfindən qeydə alındı və ən qısa zamanda çatdırılmaq üçün hazırlanacaq.
                            </p>

                            <!-- Order Meta Box -->
                            <table border='0' cellpadding='0' cellspacing='0' width='100%' style='background-color:#f8f9fa;border-radius:8px;padding:16px;margin:20px 0;border:1px solid #e9ecef;'>
                                <tr>
                                    <td width='50%' style='padding:6px;font-size:14px;color:#6c757d;'>Sifariş Nömrəsi:</td>
                                    <td width='50%' style='padding:6px;font-size:14px;font-weight:bold;color:#198754;'>#{order.OrderNumber}</td>
                                </tr>
                                <tr>
                                    <td style='padding:6px;font-size:14px;color:#6c757d;'>Tarix:</td>
                                    <td style='padding:6px;font-size:14px;font-weight:600;'>{order.CreatedDate:dd.MM.yyyy HH:mm}</td>
                                </tr>
                                <tr>
                                    <td style='padding:6px;font-size:14px;color:#6c757d;'>Ödəniş Üsulu:</td>
                                    <td style='padding:6px;font-size:14px;font-weight:600;'>{paymentMethodText}</td>
                                </tr>
                                <tr>
                                    <td style='padding:6px;font-size:14px;color:#6c757d;'>Status:</td>
                                    <td style='padding:6px;font-size:14px;'>{statusBadge}</td>
                                </tr>
                            </table>

                            <!-- Items Table -->
                            <h3 style='font-size:17px;color:#212529;margin-bottom:12px;border-bottom:2px solid #e9ecef;padding-bottom:8px;'>Sifariş Edilən Məhsullar</h3>
                            <table border='0' cellpadding='8' cellspacing='0' width='100%' style='border-collapse:collapse;margin-bottom:20px;font-size:14px;'>
                                <thead>
                                    <tr style='background-color:#eef2f7;color:#495057;'>
                                        <th align='left' style='padding:10px;border-bottom:2px solid #dee2e6;'>Məhsul</th>
                                        <th align='center' style='padding:10px;border-bottom:2px solid #dee2e6;'>Say</th>
                                        <th align='right' style='padding:10px;border-bottom:2px solid #dee2e6;'>Qiymət</th>
                                        <th align='right' style='padding:10px;border-bottom:2px solid #dee2e6;'>Məbləğ</th>
                                    </tr>
                                </thead>
                                <tbody>");

            decimal itemsSubtotal = 0;
            foreach (var item in order.OrderItems)
            {
                var productName = item.Product?.Name ?? $"Məhsul #{item.ProductId}";
                var variantText = "";
                if (item.ProductVariant != null)
                {
                    var details = new List<string>();
                    if (!string.IsNullOrEmpty(item.ProductVariant.Size)) details.Add(item.ProductVariant.Size);
                    if (!string.IsNullOrEmpty(item.ProductVariant.Color)) details.Add(item.ProductVariant.Color);
                    if (details.Any())
                    {
                        variantText = $"<br><small style='color:#6c757d;'>({string.Join(" / ", details)})</small>";
                    }
                }

                var lineTotal = item.Quantity * item.UnitPrice;
                itemsSubtotal += lineTotal;

                sb.Append($@"
                                    <tr style='border-bottom:1px solid #e9ecef;'>
                                        <td style='padding:10px;'><strong>{productName}</strong>{variantText}</td>
                                        <td align='center' style='padding:10px;'>{item.Quantity}</td>
                                        <td align='right' style='padding:10px;'>{item.UnitPrice:N2} AZN</td>
                                        <td align='right' style='padding:10px;font-weight:600;'>{lineTotal:N2} AZN</td>
                                    </tr>");
            }

            sb.Append($@"
                                </tbody>
                            </table>

                            <!-- Price Calculation Summary -->
                            <table border='0' cellpadding='4' cellspacing='0' width='100%' style='margin-bottom:25px;font-size:14px;'>
                                <tr>
                                    <td align='right' width='70%' style='color:#6c757d;padding:4px;'>Məhsulların Qiyməti:</td>
                                    <td align='right' width='30%' style='font-weight:600;padding:4px;'>{itemsSubtotal:N2} AZN</td>
                                </tr>");

            if (!string.IsNullOrEmpty(order.CouponCode) && order.DiscountAmount > 0)
            {
                sb.Append($@"
                                <tr>
                                    <td align='right' style='color:#198754;padding:4px;'>Kupon Endirimi ({order.CouponCode}):</td>
                                    <td align='right' style='color:#198754;font-weight:600;padding:4px;'>-{order.DiscountAmount:N2} AZN</td>
                                </tr>");
            }

            sb.Append($@"
                                <tr>
                                    <td align='right' style='color:#6c757d;padding:4px;'>Çatdırılma:</td>
                                    <td align='right' style='color:#198754;font-weight:600;padding:4px;'>Pulsuz</td>
                                </tr>
                                <tr>
                                    <td align='right' style='padding:10px 4px;font-size:16px;font-weight:bold;color:#212529;border-top:2px solid #212529;'>Yekun Məbləğ:</td>
                                    <td align='right' style='padding:10px 4px;font-size:18px;font-weight:bold;color:#198754;border-top:2px solid #212529;'>{order.TotalPrice:N2} AZN</td>
                                </tr>
                            </table>

                            <!-- Delivery Address Box -->
                            <h3 style='font-size:16px;color:#212529;margin-bottom:10px;border-bottom:1px solid #e9ecef;padding-bottom:6px;'>Çatdırılma Məlumatları</h3>
                            <div style='background-color:#f8f9fa;border-radius:8px;padding:14px;font-size:14px;line-height:1.6;color:#495057;'>
                                <div><strong>Ünvan:</strong> {order.Address}, {order.City}</div>
                                <div><strong>Telefon:</strong> {order.Phone}</div>
                                <div><strong>Email:</strong> {order.Email}</div>
                            </div>
                        </td>
                    </tr>

                    <!-- Footer -->
                    <tr>
                        <td style='background-color:#212529;padding:24px;text-align:center;color:#adb5bd;font-size:13px;line-height:1.5;'>
                            <div style='margin-bottom:8px;color:#ffffff;font-weight:600;'>PetShop - Sevimli Dostlarınız Üçün Ən Yaxşısı!</div>
                            <div>Hər hansı bir sualınız və ya təklifiniz olarsa, bizimlə əlaqə saxlaya bilərsiniz: <a href='mailto:support@petshop.az' style='color:#20c997;text-decoration:none;'>support@petshop.az</a></div>
                            <div style='margin-top:12px;font-size:12px;opacity:0.75;'>© {DateTime.UtcNow.Year} PetShop Azerbaijan. Bütün hüquqlar qorunur.</div>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>");

            var subject = $"🐾 PetShop - Sifarişiniz Təsdiqləndi (#{order.OrderNumber})";
            await SendEmailAsync(order.Email, subject, sb.ToString());
        }
    }
}
