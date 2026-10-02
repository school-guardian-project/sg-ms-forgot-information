namespace ms_forgot_information.Api.Phone.Application.Dto;

public sealed record RequestPhoneChangeDto(string NewPhone);

public sealed record ConfirmPhoneChangeDto(string Code);
