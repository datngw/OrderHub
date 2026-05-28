namespace OrderHub.Application.Common.Security;

public static class AuthorizationPolicies
{
    public static class Policies
    {
        public const string AdminOnly = nameof(AdminOnly);
    }

    public static class Roles
    {
        public const string Admin = "Admin";
        public const string Customer = "Customer";
    }
}
