using System.Threading.Tasks;

namespace InvoiceSystem.Application.Common.Interfaces;

public interface IEmailSender
{
    Task SendAsync(string to, string subject, string body);
}
