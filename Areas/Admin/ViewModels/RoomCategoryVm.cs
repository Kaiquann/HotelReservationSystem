namespace HotelReservationSystem.Areas.Admin.ViewModels;

public class CreateRoomCategoryVm
{
    [DisplayName("Room Category Type")] public string RoomCategoryType { get; set; } = string.Empty;

    [DisplayName("Price")] public decimal Price { get; set; }

    [DisplayName("Max People")] public int MaxPeople { get; set; }

    [DisplayName("Description")] public string Description { get; set; } = string.Empty;
    [DisplayName("Add Bed")] public bool IsAddBed { get; set; } = false;

    public List<IFormFile>? Images { get; set; }
}

public class EditRoomCategoryVm
{
    public RoomCategory RoomCategory { get; set; } = null!;

    public List<IFormFile>? Images { get; set; }
    public List<RoomImage> RoomImageList { get; set; } = [];
}