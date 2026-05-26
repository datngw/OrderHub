namespace OrderHub.Api.Features.Auth.Requests;

public record LoginRequest(
    string Email,
    string Password);
