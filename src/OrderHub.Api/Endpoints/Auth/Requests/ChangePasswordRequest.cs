namespace OrderHub.Api.Endpoints.Auth.Requests;

public record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword);
