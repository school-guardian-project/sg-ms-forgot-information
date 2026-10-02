namespace ms_forgot_information.Api.Email.Application.Dto;

public sealed record RequestEmailChangeDto(string NewEmail);

public sealed record ConfirmEmailChangeDto(string Code);
