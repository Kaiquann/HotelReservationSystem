using Microsoft.EntityFrameworkCore;

namespace HotelReservationSystem.Controllers;

[Authorize(Policy = AuthorizationPolicies.UserOnly)]
public class CartController : Controller
{
    private readonly DB _db;
    private readonly IRoomCategoryService _roomCategoryService;
    private readonly IRoomService _roomService;

    public CartController(DB db, IRoomService roomService,
        IRoomCategoryService roomCategoryService)
    {
        _db = db;
        _roomService = roomService;
        _roomCategoryService = roomCategoryService;
    }

    public async Task<IActionResult> Index()
    {
        User? currentUser = await User.GetCurrentUser(_db);
        if (currentUser == null)
        {
            TempData[SweetAlert.Danger] = Common.PleaseLoginToContinueErrorMessage;
            return RedirectToAction("Index", "Home");
        }

        List<Cart> cartList = await _db.Carts
            .Include(c => c.RoomCategory)
            .Include(c => c.RoomSetting)
            .Where(c => c.UserId == currentUser.Id)
            .ToListAsync();

        cartList = cartList.Where(c => c.RoomCategory.IsDeleted == 0).ToList();

        List<RoomImage> roomImageList = _db.RoomImages.ToList();

        bool hasIssues = false;
        List<Cart> validCartList = new();

        foreach (Cart cart in cartList)
        {
            RoomItem roomItem = new()
            {
                RoomCategoryId = cart.RoomCategoryId,
                Preference = GetPreference(cart),
                IsAddBed = cart.IsAddBed,
                Quantity = cart.Quantity,
                IsCornerRoom = cart.RoomSetting.IsCornerRoom,
                CheckInDate = cart.CheckInDate.Value,
                CheckOutDate = cart.CheckOutDate.Value
            };

            bool isRoomAvailable = await _roomService.IsRoomAvailableAsync(roomItem);
            if (!isRoomAvailable)
            {
                _db.Carts.Remove(cart);
                hasIssues = true;
                continue;
            }

            RoomCategory? roomCategory = await _roomCategoryService.GetRoomCategoryAsync(roomItem.RoomCategoryId);
            List<Room> rooms = await _roomService.GetRoomsWithPreferenceAsync(roomCategory, roomItem);
            if (rooms.Count < cart.Quantity)
            {
                hasIssues = true;
                continue;
            }

            validCartList.Add(cart);
        }

        if (hasIssues)
        {
            await _db.SaveChangesAsync();
            TempData[SweetAlert.Danger] =
                "Some items in your cart were removed because they were not available. Please review your cart.";
        }

        CartVm cartVm = new()
        {
            CartList = validCartList,
            RoomImageList = roomImageList
        };

        if (Request.IsAjax())
            return PartialView("_Index", cartVm);

        return View(cartVm);
    }


    [HttpPost]
    public async Task<IActionResult> UpdateCartData(int cartId, string quantityAction)
    {
        User? currentUser = await User.GetCurrentUser(_db);
        if (currentUser == null)
        {
            TempData[SweetAlert.Danger] = Common.PleaseLoginToContinueErrorMessage;
            return RedirectToAction("Index", "Home");
        }

        Cart? cart = await _db.Carts
            .Include(c => c.RoomCategory)
            .Include(c => c.RoomSetting)
            .FirstOrDefaultAsync(c => c.Id == cartId);

        if (cart == null)
        {
            TempData[SweetAlert.Warning] = "Cart item not found.";
            return RedirectToAction("Index");
        }

        if (quantityAction != "increase" && quantityAction != "decrease")
        {
            TempData[SweetAlert.Warning] = "Invalid action.";
            return RedirectToAction("Index");
        }

        DateTime checkInDateTime = cart.CheckInDate.GetValueOrDefault().AdjustCheckInTime();
        DateTime checkOutDateTime = cart.CheckOutDate.GetValueOrDefault().AdjustCheckOutTime();

        decimal cartDefaultPrice = cart.TotalPrice / cart.Quantity;
        if (quantityAction == "increase")
        {
            RoomItem roomItem = new()
            {
                RoomCategoryId = cart.RoomCategory.Id,
                Preference = GetPreference(cart),
                IsAddBed = cart.IsAddBed,
                IsCornerRoom = cart.RoomSetting.IsCornerRoom,
                Quantity = 1,
                CheckInDate = checkInDateTime,
                CheckOutDate = checkOutDateTime
            };

            RoomCategory? roomCategory = await _roomCategoryService.GetRoomCategoryAsync(roomItem.RoomCategoryId);

            List<Room> rooms = await _roomService.GetRoomsWithPreferenceAsync(roomCategory, roomItem);

            if (rooms.Count <= cart.Quantity)
            {
                TempData[SweetAlert.Danger] = "Sorry,We are not enough room";
                return Request.RedirectToReferer();
            }

            List<Room> availableRooms = await _roomService.GetRoomsAvailableAsync(roomItem);

            List<Room> matchingAvailableRooms = availableRooms
                .Where(r => r.RoomCategoryId == cart.RoomCategoryId)
                .ToList();

            if (cart.Quantity >= matchingAvailableRooms.Count)
            {
                TempData[SweetAlert.Danger] =
                    "Sorry, no more rooms are available for this category, only have this date";
                return Request.RedirectToReferer();
            }

            cart.Quantity += 1;
            cart.TotalPrice = cartDefaultPrice * cart.Quantity;
        }
        else if (quantityAction == "decrease" && cart.Quantity > 1)
        {
            cart.Quantity -= 1;
            cart.TotalPrice = cartDefaultPrice * cart.Quantity;
        }

        await _db.SaveChangesAsync();
        return Request.RedirectToReferer();
    }

    [HttpPost]
    public async Task<IActionResult> RemoveCart(string id)
    {
        User? currentUser = await User.GetCurrentUser(_db);
        if (currentUser == null)
        {
            TempData[SweetAlert.Danger] = Common.PleaseLoginToContinueErrorMessage;
            return RedirectToAction("Index", "Home");
        }

        if (!ModelState.IsValid)
        {
            TempData[SweetAlert.Danger] = Common.InvalidRequestErrorMessage;
            return Request.RedirectToReferer();
        }

        Cart? cart = await _db.Carts.FirstOrDefaultAsync(c => c.Id.ToString() == id);
        if (cart == null)
        {
            TempData[SweetAlert.Warning] = "Cart item not found.";
            return RedirectToAction("Index");
        }

        _db.Carts.Remove(cart);
        await _db.SaveChangesAsync();

        TempData[SweetAlert.Success] = "Successful";
        return Request.RedirectToReferer();
    }

    public decimal CalculateTotalPrice(RoomCategory roomCategory, ViewRoomVm? viewRoomVm, bool isCornerRoom,
        bool IsAddBed)
    {
        TimeSpan timeDifference = viewRoomVm.CheckOutDate.Value - viewRoomVm.CheckInDate.Value;

        double numberOfNights = timeDifference.TotalDays;
        decimal numberOfNightsDecimal = (decimal)numberOfNights;

        decimal totalPrice = roomCategory.Price * numberOfNightsDecimal +
                             (isCornerRoom ? Common.CornerRoomPrice : 0) +
                             (IsAddBed ? Common.AddBedPrice : 0);

        return totalPrice;
    }

    public string GetPreference(Cart cart)
    {
        if (cart.RoomSetting.IsLandscapeArea) return Preference.LandscapeView;

        return Preference.SmokingArea;
    }
}