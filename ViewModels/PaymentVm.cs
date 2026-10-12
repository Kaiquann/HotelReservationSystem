namespace HotelReservationSystem.ViewModels;

public class BuyNowVm
{
    public int Id { get; set; } = 0;
    public string Preference { get; set; } = string.Empty;
    public bool IsAddBed { get; set; } = false;
    public bool IsCornerRoom { get; set; } = false;
    public DateTime CheckInDate { get; set; } = DateTime.Now;
    public DateTime CheckOutDate { get; set; } = DateTime.Now;
}

// Use in Receipt and Payment Display
public class ReceiptVm
{
    public Order Order { get; set; } = null!;
    public Transaction Transaction { get; set; } = null!;
    public ICollection<Booking> Bookings { get; set; } = [];
}