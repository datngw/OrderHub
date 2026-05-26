namespace OrderHub.Domain.Users;

public static class UserConstraints
{
    public const int EmailMaxLength = 256;
    public const int FullNameMaxLength = 200;
    public const int PasswordMinLength = 8;
}
