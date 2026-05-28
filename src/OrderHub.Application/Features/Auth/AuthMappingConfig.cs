using Mapster;
using OrderHub.Domain.Users;

namespace OrderHub.Application.Features.Auth;

public sealed class AuthMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<User, AuthResponse>()
            .Map(dest => dest.Role, src => src.Role.ToString())
            .Map(dest => dest.AccessToken, src => string.Empty)
            .Map(dest => dest.RefreshToken, src => string.Empty);
    }
}
