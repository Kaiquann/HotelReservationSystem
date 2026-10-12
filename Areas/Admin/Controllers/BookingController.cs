using Microsoft.AspNetCore.Mvc.Rendering;

namespace HotelReservationSystem.Areas.Admin.Controllers;

[Authorize(Policy = AuthorizationPolicies.RequireAdminOrHigher)]
[Area("Admin")]
public class BookingController : Controller
{
    private readonly IBookingService _bookingService;
    private readonly DB _db;
    private readonly IRoomService _roomService;

    public BookingController(DB db, IBookingService bookingService, IRoomService roomService)
    {
        _db = db;
        _bookingService = bookingService;
        _roomService = roomService;
    }

    public async Task<IActionResult> Index(string? search, string? sort, string? dir, string? filter, int page = 1)
    {
        ViewBag.Status = EnumHelper.ToSelectList<BookingStatus>(sort);
        ViewBag.Search = search = search?.Trim() ?? "";
        ViewBag.Filter = filter = filter?.Trim() ?? "";

        List<Booking> bookings = await _bookingService.GetBookingsAsync();

        List<Booking> searched = bookings
            .Where(b => b.Id.ToString().Contains(search) ||
                        b.Order.OrderNumber.Contains(search) ||
                        b.Room.RoomCode.Contains(search) ||
                        b.Room.RoomCategory.Type.Contains(search) ||
                        b.Room.RoomSetting.GetRoomPreference().Contains(search) ||
                        b.CheckInDate.ToString().Contains(search) ||
                        b.CheckOutDate.ToString().Contains(search))
            .Where(b => string.IsNullOrEmpty(filter) || b.Status.ToString() == filter)
            .ToList();

        if (page < 1) return RedirectToAction(null, new { search, sort, dir, filter, page = 1 });

        IPagedList<Booking> model = SortAndPaginateBookings(searched, sort, dir, page);

        if (page > model.PageCount && model.PageCount > 0)
            return RedirectToAction(null, new { search, sort, dir, filter, page = model.PageCount });

        if (Request.IsAjax()) return PartialView("_Index", model);

        return View(model);
    }

    public async Task<IActionResult> Show(string id)
    {
        if (!ModelState.IsValid)
        {
            TempData[SweetAlert.Danger] = Common.InvalidRequestErrorMessage;
            return Request.RedirectToReferer();
        }

        Booking? booking = await _bookingService.GetBookingAsync(id);
        if (booking == null)
        {
            TempData[SweetAlert.Danger] = "Booking not found!";
            return RedirectToAction("Index");
        }

        if (Request.IsAjax()) return PartialView("_Show", booking);

        return View(booking);
    }

    public async Task<IActionResult> Edit(string id)
    {
        if (!ModelState.IsValid)
        {
            TempData[SweetAlert.Danger] = Common.InvalidRequestErrorMessage;
            return Request.RedirectToReferer();
        }

        Booking? booking = await _bookingService.GetBookingAsync(id);
        if (booking == null)
        {
            TempData[SweetAlert.Danger] = "Booking not found!";
            return RedirectToAction("Index");
        }

        if (!booking.IsBookingAllowToEdit())
        {
            TempData[SweetAlert.Danger] = "Only confirmed booking(s) can be edited";
            return RedirectToAction("Index");
        }

        RoomItem roomItem = new()
        {
            RoomCategoryId = booking.Room.RoomCategoryId,
            Preference = booking.Room.RoomSetting.GetRoomPreference(),
            IsAddBed = booking.IsAddBed,
            IsCornerRoom = booking.Room.RoomSetting.IsCornerRoom,
            Quantity = 1,
            CheckInDate = booking.CheckInDate,
            CheckOutDate = booking.CheckOutDate
        };
        List<Room> rooms = await _roomService.GetRoomsAvailableAsync(roomItem);
        SelectList roomSelectList = new SelectList(rooms, "Id", "RoomCode");
        SelectList bookingStatusList = EnumHelper.ToSelectListWithAllowed(booking.Status.ToString(),
            [BookingStatus.Pending, BookingStatus.Confirmed]);

        ViewBag.RoomList = roomSelectList;
        ViewBag.BookingStatusList = bookingStatusList;

        if (Request.IsAjax()) return PartialView("_Edit", booking);

        return View(booking);
    }

    [HttpPost]
    public async Task<IActionResult> Update(EditBookingVm editBookingVm)
    {
        try
        {
            if (!ModelState.IsValid) throw new Exception(Common.InvalidRequestErrorMessage);

            Booking? booking = await _bookingService.GetBookingAsync(editBookingVm.Id);
            if (booking == null) throw new Exception("Booking Not Found");

            if (!booking.IsBookingAllowToEdit())
                throw new Exception("Only confirmed booking(s) can be edited");

            if (editBookingVm.RoomId == booking.Room.Id.ToString()) throw new Exception("No changes detected");

            RoomItem roomItem = new()
            {
                RoomCategoryId = booking.Room.RoomCategoryId,
                Preference = booking.Room.RoomSetting.GetRoomPreference(),
                IsAddBed = booking.IsAddBed,
                IsCornerRoom = booking.Room.RoomSetting.IsCornerRoom,
                Quantity = 1,
                CheckInDate = booking.CheckInDate,
                CheckOutDate = booking.CheckOutDate
            };
            List<Room> rooms = await _roomService.GetRoomsAvailableAsync(roomItem);

            if (rooms.All(r => r.Id.ToString() != editBookingVm.RoomId))
                throw new Exception("Room Not Available");

            Room? room = rooms
                .FirstOrDefault(r => r.Id.ToString() == editBookingVm.RoomId);
            if (room == null) throw new Exception("Room Not Found");

            booking.RoomNumber = room.RoomCode;
            booking.RoomId = room.Id;
            await _db.SaveChangesAsync();

            TempData[SweetAlert.Success] = "Booking Updated Successfully";
        }
        catch (Exception ex)
        {
            TempData[SweetAlert.Danger] = ex.Message;
        }

        return Request.RedirectToReferer();
    }

    public IActionResult ScanQr()
    {
        return View();
    }

    public async Task<IActionResult> ScanQrCheckIn(string data)
    {
        try
        {
            if (!ModelState.IsValid) throw new Exception(Common.InvalidRequestErrorMessage);

            BookingQrCodeVm? bookingQrCodeVm = Decrypt<BookingQrCodeVm>(data);
            if (bookingQrCodeVm == null) throw new Exception("Invalid QR code");

            await _bookingService.SetBookingCheckInAsync([bookingQrCodeVm.BookingId.ToString()]);

            TempData[SweetAlert.Success] = "Booking checked in successfully";

            return RedirectToAction("Show", new { id = bookingQrCodeVm.BookingId });
        }
        catch (Exception ex)
        {
            TempData[SweetAlert.Danger] = ex.Message;
            return RedirectToAction("Index");
        }
    }

    public async Task<IActionResult> ScanQrCheckOut(string data)
    {
        try
        {
            if (!ModelState.IsValid) throw new Exception(Common.InvalidRequestErrorMessage);

            BookingQrCodeVm? bookingQrCodeVm = Decrypt<BookingQrCodeVm>(data);
            if (bookingQrCodeVm == null) throw new Exception("Invalid QR code");

            await _bookingService.SetBookingCheckOutAsync([bookingQrCodeVm.BookingId.ToString()]);

            TempData[SweetAlert.Success] = "Booking checked out successfully";

            return RedirectToAction("Show", new { id = bookingQrCodeVm.BookingId });
        }
        catch (Exception ex)
        {
            TempData[SweetAlert.Danger] = ex.Message;
            return RedirectToAction("Index");
        }
    }

    [HttpPost]
    public async Task<IActionResult> CheckIn(string[] ids)
    {
        try
        {
            if (!ModelState.IsValid || ids.Length == 0) throw new Exception(Common.InvalidRequestErrorMessage);

            await _bookingService.SetBookingCheckInAsync(ids);

            TempData[SweetAlert.Success] = $"Total {ids.Length} booking(s) checked in successfully";
        }
        catch (Exception ex)
        {
            TempData[SweetAlert.Danger] = ex.Message;
        }

        return Request.RedirectToReferer();
    }

    [HttpPost]
    public async Task<IActionResult> CheckOut(string[] ids)
    {
        try
        {
            if (!ModelState.IsValid || ids.Length == 0) throw new Exception(Common.InvalidRequestErrorMessage);

            await _bookingService.SetBookingCheckOutAsync(ids);

            TempData[SweetAlert.Success] = $"Total {ids.Length} booking(s) checked out successfully";
        }
        catch (Exception ex)
        {
            TempData[SweetAlert.Danger] = ex.Message;
        }

        return Request.RedirectToReferer();
    }

    /**
     * @description: Sort and Pagination
     */
    private IPagedList<Booking> SortAndPaginateBookings(List<Booking> searched, string? sort, string? dir, int page = 1)
    {
        // (2) Sorting --------------------------
        ViewBag.Sort = sort;
        ViewBag.Dir = dir;

        Func<Booking, object> fn = sort switch
        {
            "Id" => s => s.Id,
            "Order Number" => s => s.Order.OrderNumber,
            "Room Number" => s => s.Room.RoomCode,
            "Room Type" => s => s.Room.RoomCategory.Type,
            "Status" => s => s.Status,
            "Is Add Bed?" => s => s.IsAddBed,
            "Preference" => s => s.Room.RoomSetting.GetRoomPreference(),
            "Is Corner Room?" => s => s.Room.RoomSetting.IsCornerRoom,
            "Check In Date" => s => s.CheckInDate,
            "Check Out Date" => s => s.CheckOutDate,
            "When Check In" => s => s.WhenCheckInDateTime.GetValueOrDefault(),
            "When Check Out" => s => s.WhenCheckOutDateTime.GetValueOrDefault(),
            _ => s => s.Id
        };

        IOrderedEnumerable<Booking> sorted = dir == "des"
            ? searched.AsEnumerable().OrderByDescending(fn)
            : searched.AsEnumerable().OrderBy(fn);

        // (3) Paging ---------------------------
        IPagedList<Booking> pagedList = sorted.ToPagedList(page, 10);

        return pagedList;
    }
}