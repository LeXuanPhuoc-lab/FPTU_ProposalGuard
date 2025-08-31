using System.Text.RegularExpressions;
using FluentValidation;
using FPTU_ProposalGuard.Application.Dtos.Users;
using FPTU_ProposalGuard.Application.Utils;
using FPTU_ProposalGuard.Domain.Common.Enums;

namespace FPTU_ProposalGuard.Application.Validations;

public class UserDtoValidator : AbstractValidator<UserDto>
{
    public   UserDtoValidator()
    {
        // Email
            RuleFor(u => u.Email)
                .EmailAddress()
                .WithMessage("Email không hợp lệ");
            // FirstName
            RuleFor(u => u.FirstName)
                .NotEmpty()
                .Matches(@"^([A-ZÀ-Ỵ][a-zà-ỵ]*)(\s[A-ZÀ-Ỵ][a-zà-ỵ]*)*$")
                .WithMessage("Họ phải bắt đầu bằng chữ cái viết hoa cho mỗi từ")
                .Length(1, 100)
                .WithMessage("Họ phải có độ dài từ 1 đến 100 ký tự");
            // LastName
            RuleFor(u => u.LastName)
                .NotEmpty()
                .Matches(@"^([A-ZÀ-Ỵ][a-zà-ỵ]*)(\s[A-ZÀ-Ỵ][a-zà-ỵ]*)*$")
                .WithMessage("Tên phải bắt đầu bằng chữ cái viết hoa cho mỗi từ")
                .Length(1, 100)
                .WithMessage("Tên phải có độ dài từ 1 đến 100 ký tự");
            // Dob
            RuleFor(u => u.Dob)
                .Must(dob => !dob.HasValue || DateTimeUtils.IsValidAge(dob.Value))
                .WithMessage("Ngày sinh không hợp lệ");
            // Phone
            RuleFor(p => p.Phone)
                .Cascade(CascadeMode.Stop) // Prevent further checks if null/empty
                .MinimumLength(10)
                .WithMessage("SĐT không được ít hơn 10 ký tự")
                .MaximumLength(12)
                .WithMessage("SĐT không được vượt quá 12 ký tự")
                .Matches(new Regex(@"^0\d{9,10}$"))
                .WithMessage("SĐT không hợp lệ");
            // Gender
            RuleFor(e => e.Gender)
                .Must(str => Enum.TryParse(typeof(Gender), str, out _))
                .WithMessage("Giới tính không hợp lệ");
    }
}