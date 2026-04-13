using System.Threading.Tasks;

namespace PhotoAppApi.Services
{
    public interface IEmailService
    {
        Task SendInvitationEmailAsync(string email, string firstName, string lastName, string inviterName, string groupName, string message, string inviteUrl);
    }
}
