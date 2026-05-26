namespace OrderHub.Api.Endpoints.Auth.Requests;

public record RegisterRequest(
    string Email,
    string Password,
    string FullName);
