namespace Frota360.Domain.Interfaces.Services
{
    public interface IEmailService
    {
        Task EnviarAsync(string para, string assunto, string corpoHtml);
    }
}
