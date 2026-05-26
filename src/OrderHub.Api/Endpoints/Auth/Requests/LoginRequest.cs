namespace OrderHub.Api.Endpoints.Auth.Requests;

public record LoginRequest(
    string Email,
    string Password);
