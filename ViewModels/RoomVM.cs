using System.ComponentModel.DataAnnotations;

namespace HotelReservationSystem.ViewModels;

public class RoomVm
{
    [Required(ErrorMessage = "Room type is required")]
    [Display(Name = "Room Type :")]
    public string RoomType { get; set; } = string.Empty;

    [Required(ErrorMessage = "Check-in date is required")]
    [DataType(DataType.Date, ErrorMessage = "Invalid date format")]
    [Display(Name = "Check-In Date")]
    public DateTime? CheckInDate { get; set; }

    [Required(ErrorMessage = "Check-out date is required")]
    [DataType(DataType.Date, ErrorMessage = "Invalid date format")]
    [Display(Name = "Check-Out Date")]
    public DateTime? CheckOutDate { get; set; }

    [Required(ErrorMessage = "Number of guests is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Number of guests must be at least 1")]
    [Display(Name = "Number of Guests")]
    public int Guests { get; set; }

    public List<RoomCategory> RoomCategories { get; set; } = [];
    public List<RoomImage> RoomImages { get; set; }
}

public class ViewRoomVm
{
    public RoomCategory RoomCategory { get; set; }
    public List<RoomCategoryReview> RoomCategoryReview { get; set; }
    public bool IsCornerRoom { get; set; }
    public bool isPendingReview { get; set; }
    public bool isAllowToAddReview { get; set; }
    public double AverageRating { get; set; }
    public double Percentage { get; set; }
    public int TotalReviews { get; set; }
    public string Preference { get; set; }

    [Required(ErrorMessage = "Check-in date is required")]
    [DataType(DataType.Date, ErrorMessage = "Invalid date format")]
    [Display(Name = "Check-In Date")]
    public DateTime? CheckInDate { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "Check-out date is required")]
    [DataType(DataType.Date, ErrorMessage = "Invalid date format")]
    [Display(Name = "Check-Out Date")]
    public DateTime? CheckOutDate { get; set; } = DateTime.Today.AddDays(1);
}

public class TopRoomVm
{
    public List<RoomCategory> RoomCategories { get; set; } = [];
}

public class RoomCategoryReviewVm
{
    public string Id { get; set; } = string.Empty;
    public int Rating { get; set; } = 0; // Rating value from 1 to 5
    public string ReviewContent { get; set; } = string.Empty; // Review content text
}

public class RoomReportVm
{
    public List<RoomCategory> RoomCategory { get; set; }
    public List<decimal> TotalAmount { get; set; }
    public int SelectedYear { get; set; }
    public int SelectedMonth { get; set; }
}