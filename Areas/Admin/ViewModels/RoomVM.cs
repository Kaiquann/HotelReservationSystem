namespace HotelReservationSystem.Areas.Admin.ViewModels;

public class CreateRoomVm
{
    [DisplayName("Room Category")] public int RoomCategory { get; set; }

    [DisplayName("Room Setting")] public int RoomSetting { get; set; }

    public List<RoomCategory> RoomCategoryList { get; set; } = [];
    public List<RoomSetting> RoomSettingList { get; set; } = [];
}

public class EditRoomVm
{
    public int Id { get; set; }
    public int RoomCategoryId { get; set; }
    public int RoomSettingId { get; set; }
    public List<RoomCategory> RoomCategoryList { get; set; } = [];
    public List<RoomSetting> RoomSettingList { get; set; } = [];
}