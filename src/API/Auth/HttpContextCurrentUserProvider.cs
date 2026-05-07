using Services.Interfaces;

namespace API.Auth;

/// <summary>
/// Per-request <see cref="ICurrentUserProvider"/> for the API host
/// (spec 001 § data-model.md Audit split). Reads/writes
/// <see cref="HttpContext.Items"/> so the value flows scoped-to-request
/// and never bleeds across concurrent calls. Set by
/// <c>AdminAuthenticationMiddleware</c> on a successful admin-key match.
/// </summary>
/// <remarks>
/// The GUI host continues to use the singleton <c>CurrentUserProvider</c>
/// from <c>Services</c>; only the API composition root swaps in this
/// implementation.
/// </remarks>
public class HttpContextCurrentUserProvider : ICurrentUserProvider
{
    public const string ItemKey = "spec001.currentUserId";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextCurrentUserProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public int? CurrentUserId
    {
        get => _httpContextAccessor.HttpContext?.Items[ItemKey] as int?;
        set
        {
            HttpContext? context = _httpContextAccessor.HttpContext;
            if (context is null)
            {
                return;
            }
            if (value is null)
            {
                context.Items.Remove(ItemKey);
            }
            else
            {
                context.Items[ItemKey] = value;
            }
        }
    }
}
