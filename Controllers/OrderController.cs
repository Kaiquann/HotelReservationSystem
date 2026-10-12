namespace HotelReservationSystem.Controllers;

[Authorize(Policy = AuthorizationPolicies.UserOnly)]
public class OrderController : Controller
{
    private readonly IBookingService _bookingService;
    private readonly DB _db;
    private readonly IEmailService _emailService;
    private readonly IMailerService _mailerService;
    private readonly IOrderService _orderService;
    private readonly IReceiptService _receiptService;
    private readonly ITransactionService _transactionService;

    public OrderController(DB db, IEmailService emailService, IMailerService mailerService,
        IReceiptService receiptService, ITransactionService transactionService, IOrderService orderService,
        IBookingService bookingService)
    {
        _db = db;
        _emailService = emailService;
        _mailerService = mailerService;
        _receiptService = receiptService;
        _transactionService = transactionService;
        _orderService = orderService;
        _bookingService = bookingService;
    }

    public async Task<IActionResult> Index(string? search, string? sort, string? dir, string? filter, int page = 1)
    {
        User? currentUser = await User.GetCurrentUser(_db);
        if (currentUser == null)
        {
            TempData[SweetAlert.Danger] = Common.PleaseLoginToContinueErrorMessage;
            return RedirectToAction("Index", "Home");
        }

        ViewBag.Status = EnumHelper.ToSelectList<OrderStatus>(sort);
        ViewBag.Search = search = search?.Trim() ?? "";
        ViewBag.Filter = filter = filter?.Trim() ?? "";

        List<Order> orders = await _orderService.GetOrdersAsync();

        List<Order> searched = orders
            .Where(o => (o.OrderNumber.Contains(search) ||
                         o.TransactionNumber.Contains(search) ||
                         o.Status.ToString().Contains(search) ||
                         o.ServiceCharge.ToString().Contains(search) ||
                         o.SubTotalAmount.ToString().Contains(search) ||
                         o.TotalAmount.ToString().Contains(search) ||
                         o.CreatedAt.ToString().Contains(search)) &&
                        (filter == "" || o.Status.ToString() == filter) &&
                        o.UserId == currentUser.Id)
            .ToList();

        if (page < 1) return RedirectToAction(null, new { search, sort, dir, filter, page = 1 });

        IPagedList<Order> model = SortAndPaginateOrders(searched, sort, dir, page);

        if (page > model.PageCount && model.PageCount > 0)
            return RedirectToAction(null, new { search, sort, dir, filter, page = model.PageCount });

        if (Request.IsAjax()) return PartialView("_Index", model);

        return View(model);
    }

    public async Task<IActionResult> Show(string id)
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
            return RedirectToAction("Index");
        }

        Order? order = await _orderService.GetOrderWithUserIdAsync(id, currentUser.Id.ToString());
        if (order == null)
        {
            TempData[SweetAlert.Danger] = "Order Not Found";
            return RedirectToAction("Index");
        }

        List<Booking> bookings = await _bookingService.GetBookingsWithOrderIdAsync(order.Id);
        if (bookings.Count == 0)
        {
            TempData[SweetAlert.Danger] = "Bookings Not Found";
            return RedirectToAction("Index");
        }

        ShowOrderVm showOrderVm = new()
        {
            Order = order,
            Bookings = bookings
        };

        if (Request.IsAjax()) return PartialView("_Show", showOrderVm);

        return View(showOrderVm);
    }

    [HttpPost]
    public async Task<IActionResult> Cancel(string id)
    {
        try
        {
            User? currentUser = await User.GetCurrentUser(_db);
            if (currentUser == null) throw new Exception(Common.InvalidRequestErrorMessage);

            if (!ModelState.IsValid) throw new Exception(Common.InvalidRequestErrorMessage);

            Order? order = await _orderService.GetOrderWithUserIdAsync(id, currentUser.Id.ToString());
            if (order == null) throw new Exception("Order Not Found");

            if (order.Status != OrderStatus.Confirmed) throw new Exception("Order cannot be canceled");

            // Get the first booking
            List<Booking> bookings = order.Bookings.ToList();
            if (bookings.Count == 0) throw new Exception("Bookings Not Found");

            Booking booking = bookings.First();

            // Check if the booking is within 7 days of the check-in date
            if (DateTime.Today > booking.CheckInDate.AddDays(-Common.AllowDaysToCancel))
                throw new Exception("This booking cannot be canceled because it is within 7 days of the check-in date");

            order.Status = OrderStatus.PendingRefund;
            order.UpdatedAt = DateTime.Now;
            bookings.ForEach(b => { b.Status = BookingStatus.PendingRefund; });

            await _db.SaveChangesAsync();

            Transaction? transaction = await _transactionService.GetTransactionAsync(order.TransactionNumber);
            if (transaction == null) throw new Exception("Transaction Not Found");

            ReceiptVm receiptVm =
                await _receiptService.GetReceiptAsync(transaction, order);

            string body = await _emailService.OrderCancelRequest(receiptVm);

            // Send email
            Mail mail = new()
            {
                To = [currentUser.Email],
                Subject = EmailDesign.OrderCancelRequestSubject,
                Body = body
            };
            await _mailerService.SendEmail(mail);

            TempData[SweetAlert.Success] =
                "Order has been cancelled, Please wait our staff to validate and do the refund.";
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
    private IPagedList<Order> SortAndPaginateOrders(List<Order> searched, string? sort, string? dir, int page = 1)
    {
        // (2) Sorting --------------------------
        ViewBag.Sort = sort;
        ViewBag.Dir = dir;

        Func<Order, object> fn = sort switch
        {
            "Order Number" => s => s.OrderNumber,
            "Transaction Number" => s => s.TransactionNumber,
            "Status" => s => s.Status,
            "Service Charge" => s => s.ServiceCharge,
            "SubTotal" => s => s.SubTotalAmount,
            "Total Amount" => s => s.TotalAmount,
            "Created At" => s => s.CreatedAt,
            _ => s => s.Id
        };

        IOrderedEnumerable<Order> sorted = dir == "des"
            ? searched.AsEnumerable().OrderByDescending(fn)
            : searched.AsEnumerable().OrderBy(fn);

        // (3) Paging ---------------------------
        IPagedList<Order> orderPagedList = sorted.ToPagedList(page, 10);

        return orderPagedList;
    }
}