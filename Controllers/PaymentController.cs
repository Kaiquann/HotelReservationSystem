using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;

namespace HotelReservationSystem.Controllers;

[Authorize(Policy = AuthorizationPolicies.UserOnly)]
public class PaymentController : Controller
{
    private const string PaymentData = "PaymentData";
    private readonly DB _db;
    private readonly IOrderService _orderService;
    private readonly IPaymentService _paymentService;
    private readonly IReceiptService _receiptService;
    private readonly IRoomCategoryService _roomCategoryService;
    private readonly IRoomService _roomService;
    private readonly IStripeService _stripeService;
    private readonly ITransactionService _transactionService;

    public PaymentController(DB db, IStripeService stripeService, IPaymentService paymentService,
        IRoomService roomService, ITransactionService transactionService, IRoomCategoryService roomCategoryService,
        IReceiptService receiptService, IOrderService orderService)
    {
        _db = db;
        _stripeService = stripeService;
        _paymentService = paymentService;
        _roomService = roomService;
        _transactionService = transactionService;
        _roomCategoryService = roomCategoryService;
        _receiptService = receiptService;
        _orderService = orderService;
    }

    [HttpPost]
    public async Task<IActionResult> BuyNow(BuyNowVm buyNowVm)
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

        if (buyNowVm.CheckInDate < DateTime.Today || buyNowVm.CheckInDate >= buyNowVm.CheckOutDate)
        {
            TempData[SweetAlert.Danger] = "Check in date cannot over check out date!";
            return Request.RedirectToReferer();
        }

        RoomItem roomItem = new()
        {
            RoomCategoryId = buyNowVm.Id,
            Preference = buyNowVm.Preference,
            IsAddBed = buyNowVm.IsAddBed,
            IsCornerRoom = buyNowVm.IsCornerRoom,
            Quantity = 1,
            CheckInDate = buyNowVm.CheckInDate,
            CheckOutDate = buyNowVm.CheckOutDate
        };

        bool isRoomAvailable = await _roomService.IsRoomAvailableAsync(roomItem);
        if (!isRoomAvailable)
        {
            TempData[SweetAlert.Danger] = Common.NoAvailableRoomErrorMessage;
            return Request.RedirectToReferer();
        }

        PaymentRequest paymentRequest = new();
        paymentRequest.RoomItems.Add(roomItem);

        HttpContext.Session.Set(PaymentData, paymentRequest);
        return await Create(paymentRequest);
    }

    [HttpPost]
    public async Task<IActionResult> BuyWithCart(string[] ids)
    {
        User? currentUser = await User.GetCurrentUser(_db);
        if (currentUser == null)
        {
            TempData[SweetAlert.Danger] = Common.PleaseLoginToContinueErrorMessage;
            return RedirectToAction("Index", "Home");
        }

        if (ids.Length == 0)
        {
            TempData[SweetAlert.Danger] = Common.InvalidRequestErrorMessage;
            return Request.RedirectToReferer();
        }

        List<Cart> carts = await _db.Carts
            .Include(c => c.RoomCategory)
            .Include(c => c.RoomSetting)
            .Where(c => c.UserId == currentUser.Id &&
                        ids.Contains(c.Id.ToString()))
            .ToListAsync();

        if (carts.Count == 0)
        {
            TempData[SweetAlert.Danger] = "No items to checkout";
            return Request.RedirectToReferer();
        }

        // Check if all check-in and check-out dates are the same
        DateTime? checkInDate = carts.First().CheckInDate;
        DateTime? checkOutDate = carts.First().CheckOutDate;
        if (carts.Any(cart => cart.CheckInDate != checkInDate || cart.CheckOutDate != checkOutDate))
        {
            TempData[SweetAlert.Danger] =
                "Different check-in and check-out dates are not allowed to be done in one payment.";
            return Request.RedirectToReferer();
        }

        List<RoomItem> roomItems = [];
        foreach (Cart cart in carts)
        {
            RoomItem roomItem = new()
            {
                RoomCategoryId = cart.RoomCategoryId,
                Preference = cart.RoomSetting.IsLandscapeArea ? Preference.LandscapeView : Preference.SmokingArea,
                IsAddBed = cart.IsAddBed,
                IsCornerRoom = cart.RoomSetting.IsCornerRoom,
                Quantity = cart.Quantity,
                CheckInDate = cart.CheckInDate.GetValueOrDefault(),
                CheckOutDate = cart.CheckOutDate.GetValueOrDefault()
            };
            roomItems.Add(roomItem);
        }

        bool isRoomAvailable = await _roomService.IsRoomAvailableAsync(roomItems);
        if (!isRoomAvailable)
        {
            TempData[SweetAlert.Danger] = Common.NoAvailableRoomErrorMessage;
            return Request.RedirectToReferer();
        }

        PaymentRequest paymentRequest = new()
        {
            RoomItems = roomItems
        };

        HttpContext.Session.Set(PaymentData, paymentRequest);
        return await Create(paymentRequest);
    }

    [HttpPost]
    public async Task<IActionResult> Create(PaymentRequest vm)
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

        if (vm.RoomItems.Count == 0)
        {
            TempData[SweetAlert.Danger] = "No items to checkout";
            return Request.RedirectToReferer();
        }

        // Validate the room is still available or not
        bool isRoomAvailable = await _roomService.IsRoomAvailableAsync(vm.RoomItems);
        if (!isRoomAvailable)
        {
            TempData[SweetAlert.Danger] = Common.NoAvailableRoomErrorMessage;
            return Request.RedirectToReferer();
        }

        // Create a tax rate
        TaxRateCreateOptions taxRateCreateOptions = new()
        {
            DisplayName = "Service Charge " + PaymentInit.ServiceCharge + "%",
            Inclusive = false,
            Percentage = PaymentInit.ServiceCharge,
            Jurisdiction = "MY"
        };
        TaxRate taxRate = await _stripeService.CreateTaxRateAsync(taxRateCreateOptions);

        // Prepare line items
        List<SessionLineItemOptions> lineItems = [];
        foreach (RoomItem roomItem in vm.RoomItems)
        {
            RoomCategory? roomCategory = await _roomCategoryService.GetRoomCategoryAsync(roomItem.RoomCategoryId);
            if (roomCategory == null)
            {
                TempData[SweetAlert.Danger] = "Invalid room category";
                return Request.RedirectToReferer();
            }

            decimal unitAmount = roomCategory.Price;
            if (roomItem.IsAddBed) unitAmount += Common.AddBedPrice;
            if (roomItem.IsCornerRoom) unitAmount += Common.CornerRoomPrice;

            // calculate based on how many days are booked
            int totalNight = (roomItem.CheckOutDate - roomItem.CheckInDate).Days;
            if (totalNight == 0)
            {
                TempData[SweetAlert.Danger] = Common.InvalidCheckInOutDateErrorMessage;
                return Request.RedirectToReferer();
            }

            unitAmount *= totalNight;

            // PaymentRequest description
            string description = "From " + roomItem.CheckInDate.ToString("dd MMM yyyy") +
                                 " to " + roomItem.CheckOutDate.ToString("dd MMM yyyy") +
                                 " Total nights: " + totalNight;

            SessionLineItemOptions sessionLineItemOptions = new()
            {
                PriceData = new SessionLineItemPriceDataOptions
                {
                    Currency = PaymentInit.Currency,
                    ProductData = new SessionLineItemPriceDataProductDataOptions
                    {
                        Name = roomCategory.Type,
                        Description = description
                    },
                    UnitAmount = (long?)(unitAmount * 100)
                },
                Quantity = roomItem.Quantity,
                TaxRates = [taxRate.Id]
            };

            lineItems.Add(sessionLineItemOptions);
        }

        // Create a Stripe customer
        CustomerCreateOptions customerCreateOptions = new()
        {
            Email = currentUser.Email,
            Name = $"{currentUser.FirstName} {currentUser.LastName}"
        };
        Customer customer = await _stripeService.CreateCustomerAsync(customerCreateOptions);

        // Generate success and cancel URLs
        // Encrypt user data
        string encryptUserData = Encrypt(currentUser.Id);

        // Success URL
        string successUrl = Url.Action("ProcessSuccess", "Payment", null, Request.Scheme) +
                            "?sessionId={CHECKOUT_SESSION_ID}&data=" + encryptUserData;

        // Cancel URL
        string? cancelUrl = Url.Action("ProcessCancel", "Payment", null, Request.Scheme) +
                            "?sessionId={CHECKOUT_SESSION_ID}&data=" + encryptUserData;

        // Create the checkout session
        SessionCreateOptions sessionOptions = new()
        {
            Customer = customer.Id,
            LineItems = lineItems,
            SavedPaymentMethodOptions = new SessionSavedPaymentMethodOptionsOptions
            {
                PaymentMethodSave = "enabled"
            },
            InvoiceCreation = new SessionInvoiceCreationOptions
            {
                Enabled = true
            },
            Mode = "payment",
            SuccessUrl = successUrl,
            CancelUrl = cancelUrl
        };
        Session session = await _stripeService.CreateCheckoutSessionAsync(sessionOptions);

        // Redirect to the session URL
        return Redirect(session.Url);
    }

    public async Task<IActionResult> ProcessSuccess(string? sessionId, string? data)
    {
        if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(data))
        {
            TempData[SweetAlert.Danger] = "Invalid session ID or data";
            return RedirectToAction("Index", "Home");
        }

        PaymentRequest? paymentRequest = HttpContext.Session.Get<PaymentRequest>(PaymentData);
        if (paymentRequest == null)
        {
            TempData[SweetAlert.Danger] = "Invalid payment data";
            return RedirectToAction("Index", "Home");
        }

        int? userId = Decrypt<int>(data);
        if (userId == null)
        {
            TempData[SweetAlert.Danger] = "Invalid user ID";
            return RedirectToAction("Index", "Home");
        }

        User? user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
        {
            TempData[SweetAlert.Danger] = "Invalid user";
            return RedirectToAction("Index", "Home");
        }

        string transactionNumber = "TXN" + DateTime.Now.ToString("yyyyMMddHHmmss") + new Random().Next(1000, 9999);
        Payment payment = await _paymentService.InitPayment(sessionId);

        // Create transaction
        Transaction transaction = new()
        {
            TransactionNumber = transactionNumber,
            SessionId = sessionId,
            PaymentIntentId = payment.PaymentIntentId,
            PaymentMethod = payment.PaymentMethod,
            TotalAmount = payment.Amount,
            ServiceCharge = payment.AmountTax,
            Status = TransactionStatus.Confirmed,
            CreatedAt = payment.CreatedAt.ToLocalTime(),
            UpdatedAt = payment.CreatedAt.ToLocalTime(),
            UserId = user.Id
        };

        // Create order
        Order order = new()
        {
            OrderNumber = payment.OrderNumber,
            TransactionNumber = transactionNumber,
            Status = OrderStatus.Confirmed,
            ServiceChargeRate = PaymentInit.ServiceCharge,
            ServiceCharge = payment.ServiceCharge,
            SubTotalAmount = payment.Subtotal,
            TotalAmount = payment.Amount,
            CreatedAt = payment.CreatedAt.ToLocalTime(),
            UpdatedAt = payment.CreatedAt.ToLocalTime(),
            UserId = user.Id
        };
        await _db.AddAsync(transaction);
        await _db.AddAsync(order);

        // Validate the room is still available or not
        bool isRoomAvailable = await _roomService.IsRoomAvailableAsync(paymentRequest.RoomItems);
        if (!isRoomAvailable)
        {
            TempData[SweetAlert.Danger] = Common.NoAvailableRoomErrorMessage;
            return Request.RedirectToReferer();
        }

        foreach (RoomItem roomItem in paymentRequest.RoomItems)
        {
            // Get room category
            RoomCategory? roomCategory = await _roomCategoryService.GetRoomCategoryAsync(roomItem.RoomCategoryId);
            if (roomCategory == null)
            {
                TempData[SweetAlert.Danger] = Common.InvalidRoomCategoryErrorMessage;
                return RedirectToAction("Index", "Home");
            }

            // Get available rooms with the rooms and bookings to do filtering and see if there are available rooms.
            List<Room> availableRooms = await _roomService.GetRoomsAvailableAsync(roomItem);
            if (availableRooms.Count == 0)
            {
                TempData[SweetAlert.Danger] = Common.NoAvailableRoomErrorMessage;
                return RedirectToAction("Index", "Home");
            }

            decimal unitAmount = roomCategory.Price;
            if (roomItem.IsAddBed) unitAmount += Common.AddBedPrice;
            if (roomItem.IsCornerRoom) unitAmount += Common.CornerRoomPrice;

            // calculate based on how many days are booked
            int totalNight = (roomItem.CheckOutDate - roomItem.CheckInDate).Days;
            if (totalNight == 0)
            {
                TempData[SweetAlert.Danger] = Common.InvalidCheckInOutDateErrorMessage;
                return Request.RedirectToReferer();
            }

            unitAmount *= totalNight;

            int roomQuantity = roomItem.Quantity;
            Random random = new();
            HashSet<int> assignedRoomIds = [];
            for (int i = 0; i < roomQuantity; i++)
            {
                Room room;
                // Get a random room that is not assigned yet
                do
                {
                    room = availableRooms[random.Next(0, availableRooms.Count)];
                } while (assignedRoomIds.Contains(room.Id));

                // Add the room ID to the assigned room IDs
                assignedRoomIds.Add(room.Id);

                // Create a booking (Auto assign order id, handle by EF Core)
                order.Bookings.Add(new Booking
                {
                    RoomNumber = room.RoomCode,
                    IsAddBed = roomItem.IsAddBed,
                    Status = BookingStatus.Confirmed,
                    CheckInDate = roomItem.CheckInDate,
                    CheckOutDate = roomItem.CheckOutDate,
                    TotalAmount = unitAmount,
                    UserId = user.Id,
                    RoomId = room.Id
                });
            }

            // Remove the checkout room item from cart
            _db.RemoveRange(_db.Carts
                .Where(c => c.RoomCategoryId == roomItem.RoomCategoryId)
                .Where(c => c.IsAddBed == roomItem.IsAddBed)
                .Where(c => (roomItem.Preference == Preference.LandscapeView && c.RoomSetting.IsLandscapeArea) ||
                            (roomItem.Preference == Preference.SmokingArea && c.RoomSetting.IsSmokingArea))
                .Where(c => c.RoomSetting.IsCornerRoom == roomItem.IsCornerRoom)
                .Where(c => c.Quantity == roomItem.Quantity)
            );
        }

        // Save changes
        await _db.SaveChangesAsync();

        // Send receipt
        await _receiptService.SendReceiptAsync(transaction, order);

        HttpContext.Session.Remove(PaymentData);
        return RedirectToAction("Success", new { transactionNumber = transaction.TransactionNumber });
    }

    public IActionResult ProcessCancel()
    {
        HttpContext.Session.Remove(PaymentData);
        return RedirectToAction("Cancel");
    }

    public async Task<IActionResult> Success(string transactionNumber)
    {
        Transaction? transaction = await _transactionService.GetTransactionAsync(transactionNumber);
        if (transaction == null)
        {
            TempData[SweetAlert.Danger] = "Invalid transaction number";
            return RedirectToAction("Index", "Home");
        }

        Order? order = await _orderService.GetOrderWithTransactionNumberAsync(transactionNumber);
        if (order == null)
        {
            TempData[SweetAlert.Danger] = "Invalid order number";
            return RedirectToAction("Index", "Home");
        }

        ReceiptVm receiptVm = await _receiptService.GetReceiptAsync(transaction, order);

        return View(receiptVm);
    }

    public IActionResult Cancel()
    {
        return View();
    }
}