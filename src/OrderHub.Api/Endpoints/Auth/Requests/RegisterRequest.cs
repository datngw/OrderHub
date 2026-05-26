namespace OrderHub.Api.Features.Auth.Requests;

public record RegisterRequest(
    string Email,
    string Password,
    string FullName);
