namespace HotelReservationSystem.Areas.Admin.ViewModels;

public class CreateRoomCategoryVm
{
    [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.StringLength(50)]
    [DisplayName("Room Category Type")] public string RoomCategoryType { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Range(typeof(decimal), "0.01", "9999999999999999.99")]
    [DisplayName("Price")] public decimal Price { get; set; }

    [System.ComponentModel.DataAnnotations.Range(1, 100)]
    [DisplayName("Max People")] public int MaxPeople { get; set; }

    [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.StringLength(255)]
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
