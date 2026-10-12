namespace HotelReservationSystem.Models;

public class PaymentRequest
{
    public List<RoomItem> RoomItems { get; set; } = [];
}

public class RoomItem
{
    public int RoomCategoryId { get; set; } = 0;

    public string Preference { get; set; } = string.Empty;

    public bool IsAddBed { get; set; } = false;

    public bool IsCornerRoom { get; set; } = false;

    public int Quantity { get; set; } = 1;

    public DateTime CheckInDate { get; set; } = DateTime.Now;

    public DateTime CheckOutDate { get; set; } = DateTime.Now;
}