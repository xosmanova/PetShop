using PetShop.DataContext.Entites;

namespace PetShop.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string htmlMessage);
        Task SendOrderConfirmationEmailAsync(Order order);
    }
}
