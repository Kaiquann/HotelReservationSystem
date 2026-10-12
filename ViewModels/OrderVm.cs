namespace HotelReservationSystem.ViewModels;

public class ShowOrderVm
{
    public Order Order { get; set; } = new();
    public ICollection<Booking> Bookings { get; set; } = [];
}