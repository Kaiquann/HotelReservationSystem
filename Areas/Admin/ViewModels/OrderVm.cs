using Microsoft.AspNetCore.Mvc.Rendering;

namespace HotelReservationSystem.Areas.Admin.ViewModels;

public class EditOrderVm
{
    // Form
    public string Id { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;

    // Vm
    public Order? Order { get; set; }
    public IEnumerable<Booking>? Bookings { get; set; }
    public IEnumerable<SelectListItem>? OrderStatusList { get; set; }
}