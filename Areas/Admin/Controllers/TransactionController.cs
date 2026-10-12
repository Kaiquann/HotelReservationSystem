using Microsoft.EntityFrameworkCore;

namespace HotelReservationSystem.Areas.Admin.Controllers;

[Authorize(Policy = AuthorizationPolicies.RequireAdminOrHigher)]
[Area("Admin")]
public class TransactionController : Controller
{
    private readonly DB _db;
    private readonly IOrderService _orderService;
    private readonly IReceiptService _receiptService;
    private readonly ITransactionService _transactionService;

    public TransactionController(DB db, IReceiptService receiptService, ITransactionService transactionService,
        IOrderService orderService)
    {
        _db = db;
        _receiptService = receiptService;
        _transactionService = transactionService;
        _orderService = orderService;
    }

    public async Task<IActionResult> Index(string? search, string? sort, string? dir, string? filter, int page = 1)
    {
        ViewBag.Status = EnumHelper.ToSelectList<TransactionStatus>(sort);
        ViewBag.Search = search = search?.Trim() ?? "";
        ViewBag.Filter = filter = filter?.Trim() ?? "";

        List<Transaction> searched = await _db.Transactions
            .Where(t => (t.TransactionNumber.Contains(search) ||
                         t.PaymentMethod.Contains(search) ||
                         t.ServiceCharge.ToString().Contains(search) ||
                         t.TotalAmount.ToString().Contains(search) ||
                         t.Status.ToString().Contains(search) ||
                         t.CreatedAt.ToString().Contains(search) ||
                         t.UpdatedAt.ToString().Contains(search)) &&
                        (filter == "" || t.Status.ToString() == filter))
            .ToListAsync();

        if (page < 1) return RedirectToAction(null, new { search, sort, dir, filter, page = 1 });

        IPagedList<Transaction> model = SortAndPaginateTransactions(searched, sort, dir, page);

        if (page > model.PageCount && model.PageCount > 0)
            return RedirectToAction(null, new { search, sort, dir, filter, page = model.PageCount });

        if (Request.IsAjax()) return PartialView("_Index", model);

        return View(model);
    }

    public async Task<IActionResult> Receipt(string id)
    {
        try
        {
            if (!ModelState.IsValid) throw new Exception(Common.InvalidRequestErrorMessage);

            Transaction? transaction = await _transactionService.GetTransactionAsync(id);
            if (transaction == null) throw new Exception("Transaction Not Found");

            Order? order = await _orderService.GetOrderWithTransactionNumberAsync(id);
            if (order == null) throw new Exception("Order Not Found");

            ReceiptVm receiptVm = await _receiptService.GetReceiptAsync(transaction, order);
            return View(receiptVm);
        }
        catch (Exception ex)
        {
            TempData[SweetAlert.Danger] = ex.Message;
            return RedirectToAction("Index");
        }
    }

    [HttpPost]
    public async Task<IActionResult> SendReceipt(string id)
    {
        try
        {
            if (!ModelState.IsValid) throw new Exception(Common.InvalidRequestErrorMessage);

            Transaction? transaction = await _transactionService.GetTransactionAsync(id);
            if (transaction == null) throw new Exception("Transaction Not Found");

            Order? order = await _orderService.GetOrderWithTransactionNumberAsync(id);
            if (order == null) throw new Exception("Order Not Found");

            if (order.User.IsDeleted >= 2)
                throw new Exception(
                    "The email is not sent because the user is deleted");

            await _receiptService.SendReceiptAsync(transaction, order);

            TempData[SweetAlert.Success] = "Receipt Sent Successfully";
        }
        catch (Exception ex)
        {
            TempData[SweetAlert.Danger] = ex.Message;
        }

        return PartialView("_SweetAlert");
    }

    /**
     * @description: Sort and Pagination
     */
    private IPagedList<Transaction> SortAndPaginateTransactions(List<Transaction> searched, string? sort, string? dir,
        int page = 1)
    {
        // (2) Sorting --------------------------
        ViewBag.Sort = sort;
        ViewBag.Dir = dir;

        Func<Transaction, object> fn = sort switch
        {
            "Transaction Number" => s => s.TransactionNumber,
            "Payment Method" => s => s.PaymentMethod,
            "Service Charge" => s => s.ServiceCharge,
            "Total Amount" => s => s.TotalAmount,
            "Status" => s => s.Status,
            "Created At" => s => s.CreatedAt,
            "Updated At" => s => s.UpdatedAt,
            _ => s => s.Id
        };

        IOrderedEnumerable<Transaction> sorted = dir == "des"
            ? searched.AsEnumerable().OrderByDescending(fn)
            : searched.AsEnumerable().OrderBy(fn);

        // (3) Paging ---------------------------
        IPagedList<Transaction> transactionPagedList = sorted.ToPagedList(page, 10);

        return transactionPagedList;
    }
}