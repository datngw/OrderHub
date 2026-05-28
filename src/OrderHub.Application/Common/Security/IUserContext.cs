namespace OrderHub.Application.Common.Security;

public interface IUserContext
{
    Guid UserId { get; }
    bool IsAdmin { get; }
}
