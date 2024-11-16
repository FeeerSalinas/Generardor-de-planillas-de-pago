using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using ProyectoANF.Models;
using System.Text.RegularExpressions;

namespace ProyectoANF.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly SmtpClient _smtpClient;

        public EmailService(IOptions<EmailSettings> emailSettings)
        {
            _emailSettings = emailSettings.Value;

            _smtpClient = new SmtpClient(_emailSettings.SmtpServer, _emailSettings.SmtpPort)
            {
                Credentials = new NetworkCredential(_emailSettings.SmtpUsername, _emailSettings.SmtpPassword),
                EnableSsl = true
            };
        }

        public async Task SendEmailWithAttachmentAsync(string toEmail, string subject, string body, byte[] attachment, string fileName)
        {
            try
            {
                if (!IsValidEmail(toEmail))
                {
                    throw new ArgumentException("La dirección de correo electrónico no es válida.", nameof(toEmail));
                }

                using var mailMessage = new MailMessage
                {
                    From = new MailAddress(_emailSettings.SenderEmail, _emailSettings.SenderName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(toEmail);

                // Agregar el PDF como adjunto
                if (attachment != null && attachment.Length > 0)
                {
                    using var ms = new MemoryStream(attachment);
                    var attachmentFile = new Attachment(ms, fileName, "application/pdf");
                    mailMessage.Attachments.Add(attachmentFile);

                    await _smtpClient.SendMailAsync(mailMessage);
                }
                else
                {
                    throw new ArgumentException("El archivo adjunto está vacío o es nulo.", nameof(attachment));
                }
            }
            catch (Exception ex)
            {
                // Aquí podrías agregar logging
                throw new Exception($"Error al enviar el correo electrónico: {ex.Message}", ex);
            }
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                if (!IsValidEmail(toEmail))
                {
                    throw new ArgumentException("La dirección de correo electrónico no es válida.", nameof(toEmail));
                }

                using var mailMessage = new MailMessage
                {
                    From = new MailAddress(_emailSettings.SenderEmail, _emailSettings.SenderName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(toEmail);

                await _smtpClient.SendMailAsync(mailMessage);
            }
            catch (Exception ex)
            {
                // Aquí podrías agregar logging
                throw new Exception($"Error al enviar el correo electrónico: {ex.Message}", ex);
            }
        }

        public bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                // Patrón para validar el formato del correo electrónico
                var pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
                var regex = new Regex(pattern, RegexOptions.IgnoreCase);

                return regex.IsMatch(email);
            }
            catch
            {
                return false;
            }
        }

        // Implementa IDisposable para liberar recursos
        public void Dispose()
        {
            _smtpClient?.Dispose();
        }
    }
}