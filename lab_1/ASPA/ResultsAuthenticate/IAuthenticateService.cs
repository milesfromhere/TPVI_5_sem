using Microsoft.AspNetCore.Http;

namespace Authenticate
{
    public interface IAuthenticateService
    {
        Task<bool> SignInAsync(HttpContext context, string login, string password);
        Task SignOutAsync(HttpContext context);
    }
}