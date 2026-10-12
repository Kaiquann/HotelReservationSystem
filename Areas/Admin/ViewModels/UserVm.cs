using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HotelReservationSystem.Areas.Admin.ViewModels;

public class CreateUserVm
{
    [DisplayName("First Name")] public string FirstName { get; set; } = string.Empty;

    [DisplayName("Last Name")] public string LastName { get; set; } = string.Empty;

    [EmailAddress]
    [DataType(DataType.EmailAddress)]
    [Remote("IsEmailUnique", "User", "", ErrorMessage = Common.EmailExistsErrorMessage, HttpMethod = "POST")]
    public string Email { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public IEnumerable<SelectListItem> Roles { get; set; } = [];
}

public class EditUserVm
{
    public string Id { get; set; } = string.Empty;

    [DisplayName("First Name")] public string FirstName { get; set; } = string.Empty;

    [DisplayName("Last Name")] public string LastName { get; set; } = string.Empty;

    [DisplayName("Username")] public string UserName { get; set; } = string.Empty;

    [EmailAddress]
    [DataType(DataType.EmailAddress)]
    public string Email { get; set; } = string.Empty;

    [DataType(DataType.PhoneNumber)]
    [DisplayName("Phone Number")]
    [StringLength(15, ErrorMessage = Common.ErrorMessage)]
    [RegularExpression(Common.PhoneNumber, ErrorMessage = Common.PhoneNumberErrorMessage)]
    public string? PhoneNumber { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public string? ProfileImageUrl { get; set; }

    public IFormFile? ProfileImage { get; set; }

    public bool IsBlocked { get; set; } = false;

    public int IsDeleted { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public IEnumerable<SelectListItem> Roles { get; set; } = [];
}