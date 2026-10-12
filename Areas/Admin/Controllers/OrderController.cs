using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HotelReservationSystem.Areas.Admin.Controllers;

[Authorize(Policy = AuthorizationPolicies.RequireAdminOrHigher)]
[Area("Admin")]
public class OrderController : Controller
{
    private readonly IBookingService _bookingService;
    private readonly DB _db;
    private readonly IEmailService _emailService;
    private readonly IMailerService _mailerService;
    private readonly IOrderService _orderService;
    private readonly IStripeService _stripeService;
    private readonly ITransactionService _transactionService;

    public OrderController(DB db, IEmailService emailService, IMailerService mailerService,
        IBookingService bookingService, IOrderService orderService, IStripeService stripeService,
        ITransactionService transactionService)
    {
        _db = db;
        _emailService = emailService;
        _mailerService = mailerService;
        _bookingService = bookingService;
        _orderService = orderService;
        _stripeService = stripeService;
        _transactionService = transactionService;
    }

    public async Task<IActionResult> Index(string? search, string? sort, string? dir, string? filter, int page = 1)
    {
        ViewBag.Status = EnumHelper.ToSelectList<OrderStatus>(sort);
        ViewBag.Search = search = search?.Trim() ?? "";
        ViewBag.Filter = filter = filter?.Trim() ?? "";

        List<Order> searched = await _db.Orders
            .Include(o => o.Bookings)
            .Where(o => (o.OrderNumber.Contains(search) ||
                         o.TransactionNumber.Contains(search) ||
                         o.Status.ToString().Contains(search) ||
                         o.ServiceCharge.ToString().Contains(search) ||
                         o.SubTotalAmount.ToString().Contains(search) ||
                         o.TotalAmount.ToString().Contains(search) ||
                         o.CreatedAt.ToString().Contains(search)) &&
                        (filter == "" || o.Status.ToString() == filter))
            .ToListAsync();

        if (page < 1) return RedirectToAction(null, new { search, sort, dir, filter, page = 1 });

        IPagedList<Order> model = SortAndPaginateOrders(searched, sort, dir, page);

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

        Order? order = await _orderService.GetOrderAsync(id);
        if (order == null)
        {
            TempData[SweetAlert.Danger] = "Order Not Found";
            return RedirectToAction("Index");
        }

        List<Booking> bookings = await _bookingService.GetBookingsWithOrderIdAsync(order.Id);

        ShowOrderVm showOrderVm = new()
        {
            Order = order,
            Bookings = bookings
        };

        return View(showOrderVm);
    }

    public async Task<IActionResult> Edit(string id)
    {
        if (!ModelState.IsValid)
        {
            TempData[SweetAlert.Danger] = Common.InvalidRequestErrorMessage;
            return Request.RedirectToReferer();
        }

        Order? order = await _orderService.GetOrderAsync(id);
        if (order == null)
        {
            TempData[SweetAlert.Danger] = "Order Not Found";
            return RedirectToAction("Index");
        }

        if (order.Status == OrderStatus.Refunded)
        {
            TempData[SweetAlert.Danger] = "Refunded Order Cannot Be Edited";
            return RedirectToAction("Index");
        }

        List<Booking> bookings = await _bookingService.GetBookingsWithOrderIdAsync(order.Id);
        SelectList orderStatus = EnumHelper.ToSelectListWithAllowed(order.Status.ToString(),
            [OrderStatus.Pending, OrderStatus.Confirmed, OrderStatus.Canceled]);

        EditOrderVm editOrderVm = new()
        {
            Id = order.OrderNumber,
            Order = order,
            Status = order.Status.ToString(),
            Bookings = bookings,
            OrderStatusList = orderStatus
        };

        if (Request.IsAjax()) return PartialView("_Edit", editOrderVm);

        return View(editOrderVm);
    }

    [HttpPost]
    public async Task<IActionResult> Update(EditOrderVm editOrderVm)
    {
        try
        {
            if (!ModelState.IsValid) throw new Exception(Common.InvalidRequestErrorMessage);

            Order? order = await _orderService.GetOrderAsync(editOrderVm.Id);
            if (order == null) throw new Exception("Order Not Found");

            if (!Enum.TryParse(editOrderVm.Status, out OrderStatus status)) throw new Exception("Invalid Status");

            if (!EnumHelper.IsAllowValue(order.Status,
                    [OrderStatus.Pending, OrderStatus.Confirmed, OrderStatus.Canceled]))
                throw new Exception("Only pending, confirmed or canceled order can be edited");

            if (editOrderVm.Status == OrderStatus.Refunded.ToString())
                throw new Exception("Not allowed to set order status to Refunded");

            List<Booking> bookings = await _bookingService.GetBookingsWithOrderIdAsync(order.Id);

            BookingStatus bookingStatus = status switch
            {
                OrderStatus.Pending => BookingStatus.Pending,
                OrderStatus.Confirmed => BookingStatus.Confirmed,
                OrderStatus.Canceled => BookingStatus.Canceled,
                _ => BookingStatus.Pending
            };

            bookings.ForEach(b => { b.Status = bookingStatus; });
            order.Status = status;
            order.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync();

            TempData[SweetAlert.Success] = "Order Updated Successfully";
        }
        catch (Exception ex)
        {
            TempData[SweetAlert.Danger] = ex.Message;
        }

        return Request.RedirectToReferer();
    }

    [HttpPost]
    public async Task<IActionResult> Refund(string id)
    {
        try
        {
            if (!ModelState.IsValid) throw new Exception(Common.InvalidRequestErrorMessage);

            Order? order = await _orderService.GetOrderAsync(id);
            if (order == null) throw new Exception("Order Not Found");

            if (order.Status != OrderStatus.PendingRefund)
                throw new Exception("Only pending refund order can be refunded");

            Transaction? transaction = await _transactionService.GetTransactionAsync(order.TransactionNumber);
            if (transaction == null) throw new Exception("Transaction Not Found");

            // Call Stripe API to refund payment
            await _stripeService.RefundPaymentIntentAsync(transaction.PaymentIntentId, transaction.TotalAmount);

            List<Booking> bookings = await _bookingService.GetBookingsWithOrderIdAsync(order.Id);

            // Update order
            order.Status = OrderStatus.Refunded;
            order.UpdatedAt = DateTime.Now;

            // Update transactions
            transaction.Status = TransactionStatus.Refunded;
            transaction.UpdatedAt = DateTime.Now;

            // Update bookings
            bookings.ForEach(b => { b.Status = BookingStatus.Refunded; });

            await _db.SaveChangesAsync();

            if (order.User.IsDeleted >= 2)
                throw new Exception(
                    "The refund is processed but the notification email is not sent because the user is deleted");

            ReceiptVm receiptVm = new()
            {
                Order = order,
                Transaction = transaction,
                Bookings = bookings
            };

            string subject = EmailDesign.OrderRefundedSubject + " : " + order.OrderNumber;
            string body = await _emailService.OrderRefunded(receiptVm);

            Mail mail = new Mail
            {
                To = [order.User.Email],
                Subject = subject,
                Body = body
            };
            await _mailerService.SendEmail(mail);

            TempData[SweetAlert.Success] = "Order refunded successfully";
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