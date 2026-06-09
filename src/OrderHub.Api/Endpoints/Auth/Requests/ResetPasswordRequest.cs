namespace OrderHub.Api.Endpoints.Auth.Requests;

public record ResetPasswordRequest(
    string Email,
    string Code,
    string NewPassword);
