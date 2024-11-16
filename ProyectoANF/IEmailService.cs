using System.Threading.Tasks;

namespace ProyectoANF.Services
{
    public interface IEmailService
    {
        Task SendEmailWithAttachmentAsync(string toEmail, string subject, string body, byte[] attachment, string fileName);
        Task SendEmailAsync(string toEmail, string subject, string body);
        bool IsValidEmail(string email);
    }
}