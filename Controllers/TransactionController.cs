using Microsoft.EntityFrameworkCore;

namespace HotelReservationSystem.Controllers;

[Authorize(Policy = AuthorizationPolicies.UserOnly)]
public class TransactionController : Controller
{
    private readonly DB _db;
    private readonly IOrderService _orderService;
    private readonly IReceiptService _receiptService;
    private readonly ITransactionService _transactionService;

    public TransactionController(DB db, ITransactionService transactionService, IOrderService orderService,
        IReceiptService receiptService)
    {
        _db = db;
        _transactionService = transactionService;
        _orderService = orderService;
        _receiptService = receiptService;
    }

    public async Task<IActionResult> Index(string? search, string? sort, string? dir, string? filter, int page = 1)
    {
        User? currentUser = await User.GetCurrentUser(_db);
        if (currentUser == null)
        {
            TempData[SweetAlert.Danger] = Common.PleaseLoginToContinueErrorMessage;
            return RedirectToAction("Index", "Home");
        }

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
                        (filter == "" || t.Status.ToString() == filter) &&
                        t.UserId == currentUser.Id)
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

        try
        {
            Transaction? transaction =
                await _transactionService.GetTransactionWithUserIdAsync(id, currentUser.Id.ToString());
            if (transaction == null) throw new Exception("Transaction Not Found");

            Order? order =
                await _orderService.GetOrderWithTransactionNumberAndUserIdAsync(id, currentUser.Id.ToString());
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
        User? currentUser = await User.GetCurrentUser(_db);
        if (currentUser == null)
        {
            TempData[SweetAlert.Danger] = Common.PleaseLoginToContinueErrorMessage;
            return PartialView("_SweetAlert");
        }

        if (!ModelState.IsValid)
        {
            TempData[SweetAlert.Danger] = Common.InvalidRequestErrorMessage;
            return PartialView("_SweetAlert");
        }

        try
        {
            Transaction? transaction =
                await _transactionService.GetTransactionWithUserIdAsync(id, currentUser.Id.ToString());
            if (transaction == null) throw new Exception("Transaction Not Found");

            Order? order =
                await _orderService.GetOrderWithTransactionNumberAndUserIdAsync(id, currentUser.Id.ToString());
            if (order == null) throw new Exception("Order Not Found");

            await _receiptService.SendReceiptAsync(transaction, order);

            TempData[SweetAlert.Success] = "Receipt send successfully!";
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