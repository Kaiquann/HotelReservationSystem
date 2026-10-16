using Microsoft.EntityFrameworkCore;

namespace HotelReservationSystem.Areas.Admin.Controllers;

[Authorize(Policy = AuthorizationPolicies.RequireAdminOrHigher)]
[Area("Admin")]
public class RoomCategoryController : Controller
{
    private readonly DB _db;
    private readonly IImageService _imageService;
    private readonly ILogger<RoomCategoryController> _logger;

    public RoomCategoryController(DB db, IImageService imageService, ILogger<RoomCategoryController> logger)
    {
        _db = db;
        _imageService = imageService;
        _logger = logger;
    }

    public async Task<IActionResult> Index(string? search, string? sort, string? dir, int page = 1)
    {
        ViewBag.Search = search = search?.Trim() ?? "";

        List<RoomCategory> searched = await _db.RoomCategories
            .Include(rc => rc.RoomImages)
            .Include(rc => rc.RoomCategoryReview)
            .Where(rc => (rc.Type.Contains(search) ||
                          rc.Id.ToString().Contains(search)) &&
                         rc.IsDeleted == 0)
            .ToListAsync();

        if (page < 1) return RedirectToAction(null, new { search, sort, dir, page = 1 });

        IPagedList<RoomCategory> model = SortAndPaginateRoomCategory(searched, sort, dir, page);

        if (page > model.PageCount && model.PageCount > 0)
            return RedirectToAction(null, new { search, sort, dir, page = model.PageCount });

        if (Request.IsAjax()) return PartialView("_Index", model);

        return View(model);
    }

    public async Task<IActionResult> Deleted(string? search, string? sort, string? dir, string? filter, int page = 1)
    {
        ViewBag.Search = search = search?.Trim() ?? "";

        List<RoomCategory> searched = await _db.RoomCategories
            .Where(rc => (rc.Type.Contains(search) ||
                          rc.Price.ToString().Contains(search) ||
                          rc.MaxPeople.ToString().Contains(search)) &&
                         rc.IsDeleted == 1)
            .ToListAsync();

        if (page < 1) return RedirectToAction(null, new { search, sort, dir, page = 1 });

        IPagedList<RoomCategory> model = SortAndPaginateRoomCategory(searched, sort, dir, page);

        if (page > model.PageCount && model.PageCount > 0)
            return RedirectToAction(null, new { search, sort, dir, page = model.PageCount });

        if (Request.IsAjax()) return PartialView("_Index", model);
        return View(model);
    }

    public async Task<IActionResult> Review(string? search, string? sort, string? dir, string? filter, int page = 1)
    {
        ViewBag.Search = search = search?.Trim() ?? "";

        List<RoomCategoryReview> searched = await _db.RoomCategoryReviews
            .Include(rc => rc.User)
            .Include(rc => rc.RoomCategory)
            .Where(rc => rc.Rating.ToString().Contains(search) ||
                         rc.RoomCategory.Type.ToString().Contains(search) ||
                         rc.Message.ToString().Contains(search) ||
                         rc.User.UserName.ToString().Contains(search))
            .Where(rc => rc.Status == RoomCategoryReviewStatus.PendingReview)
            .ToListAsync();

        if (page < 1) return RedirectToAction(null, new { search, sort, dir, page = 1 });

        IPagedList<RoomCategoryReview> model = SortAndPaginateRoomCategoryReview(searched, sort, dir, page);

        if (page > model.PageCount && model.PageCount > 0)
            return RedirectToAction(null, new { search, sort, dir, page = model.PageCount });

        return View(model);
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Store(CreateRoomCategoryVm createRoomCategoryVm)
    {
        if (!ModelState.IsValid)
        {
            TempData[SweetAlert.Danger] = string.Join(" ", ModelState.Values
                .SelectMany(value => value.Errors)
                .Select(error => error.ErrorMessage)
                .Where(message => !string.IsNullOrWhiteSpace(message)));
            return View("Create", createRoomCategoryVm);
        }

        string categoryType = createRoomCategoryVm.RoomCategoryType.Trim();
        categoryType = char.ToUpperInvariant(categoryType[0]) + categoryType[1..];

        bool checkName = await _db.RoomCategories
            .AnyAsync(rc => rc.Type == categoryType);

        if (checkName)
        {
            TempData[SweetAlert.Danger] = "This Room Category already exists";
            return View("Create", createRoomCategoryVm);
        }

        if (createRoomCategoryVm.Images == null || createRoomCategoryVm.Images.Count == 0)
        {
            TempData[SweetAlert.Danger] = "You need upload at least one";
            return View("Create", createRoomCategoryVm);
        }

        if (createRoomCategoryVm.Images.Count > 5)
        {
            TempData[SweetAlert.Danger] = "You can upload a maximum of 5 images.";
            return View("Create", createRoomCategoryVm);
        }

        foreach (IFormFile img in createRoomCategoryVm.Images)
        {
            string error = _imageService.ValidatePhoto(img);
            if (string.IsNullOrEmpty(error)) continue;

            TempData[SweetAlert.Danger] = $"{img.FileName}: {error}";
            return View("Create", createRoomCategoryVm);
        }

        List<string> savedImages = [];
        try
        {
            List<RoomImage>? roomImages = createRoomCategoryVm.Images?.Select(img =>
            {
                string name = _imageService.SavePhoto(img, Common.ImageUploadPath);
                savedImages.Add(name);
                return new RoomImage { ImagePath = name };
            }).ToList();

            RoomCategory newRoomCategory = new RoomCategory
            {
                Type = categoryType,
                Price = createRoomCategoryVm.Price,
                MaxPeople = createRoomCategoryVm.MaxPeople,
                Description = createRoomCategoryVm.Description,
                IsAddBed = createRoomCategoryVm.IsAddBed,
                RoomImages = roomImages ?? []
            };

            await _db.RoomCategories.AddAsync(newRoomCategory);
            await _db.SaveChangesAsync();

            TempData[SweetAlert.Success] = "Room Category Created Successfully";
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            foreach (string savedImage in savedImages)
                _imageService.DeletePhoto(savedImage, Common.ImageUploadPath);

            _logger.LogError(ex, "Failed to create room category with {ImageCount} images",
                createRoomCategoryVm.Images.Count);
            TempData[SweetAlert.Danger] = ex is DbUpdateException
                ? "The room category could not be saved because the database rejected the data. Check that the database migrations are up to date."
                : "One of the uploaded files could not be processed. Please upload a valid JPG or PNG image.";
            return View("Create", createRoomCategoryVm);
        }
    }


    public async Task<IActionResult> Edit(int id)
    {
        if (!ModelState.IsValid)
        {
            TempData[SweetAlert.Danger] = Common.InvalidRequestErrorMessage;
            return Request.RedirectToReferer();
        }

        RoomCategory? roomCategory = await _db.RoomCategories
            .Include(rc => rc.RoomImages)
            .FirstOrDefaultAsync(rc => rc.Id == id);

        if (roomCategory == null)
        {
            TempData[SweetAlert.Danger] = "Room Category not found";
            return RedirectToAction("Index");
        }

        EditRoomCategoryVm editRoomCategoryVm = new()
        {
            RoomCategory = roomCategory,
            RoomImageList = roomCategory.RoomImages.ToList()
        };

        return View(editRoomCategoryVm);
    }

    [HttpPost]
    public async Task<IActionResult> Update(EditRoomCategoryVm editRoomCategoryVm)
    {
        if (!ModelState.IsValid)
        {
            TempData[SweetAlert.Danger] = Common.InvalidRequestErrorMessage;
            return Request.RedirectToReferer();
        }

        RoomCategory? roomCategory =
            await _db.RoomCategories.FirstOrDefaultAsync(rc => rc.Id == editRoomCategoryVm.RoomCategory.Id);
        if (roomCategory == null)
        {
            TempData[SweetAlert.Danger] = "Room Category not found";
            return RedirectToAction("Index");
        }

        if (!editRoomCategoryVm.RoomCategory.Type.Equals(roomCategory.Type))
        {
            bool checkName = _db.RoomCategories
                .Any(rc => rc.Type.Equals(editRoomCategoryVm.RoomCategory.Type));
            if (checkName)
            {
                TempData[SweetAlert.Danger] = "This Room Category already exists";
                return Request.RedirectToReferer();
            }
        }

        if (editRoomCategoryVm.Images != null)
            foreach (IFormFile img in editRoomCategoryVm.Images)
            {
                string error = _imageService.ValidatePhoto(img);
                if (!string.IsNullOrEmpty(error)) ModelState.AddModelError("RoomCategoryImage", error);
            }

        try
        {
            List<RoomImage>? roomImages = editRoomCategoryVm.Images?.Select(img =>
            {
                string name = _imageService.SavePhoto(img, Common.ImageUploadPath);
                return new RoomImage { ImagePath = name };
            }).ToList();

            roomCategory.Type = editRoomCategoryVm.RoomCategory.Type;
            roomCategory.Price = editRoomCategoryVm.RoomCategory.Price;
            roomCategory.MaxPeople = editRoomCategoryVm.RoomCategory.MaxPeople;
            roomCategory.Description = editRoomCategoryVm.RoomCategory.Description;
            roomCategory.IsAddBed = editRoomCategoryVm.RoomCategory.IsAddBed;
            roomCategory.RoomImages = roomImages ?? [];

            await _db.SaveChangesAsync();

            TempData[SweetAlert.Success] = "Room Category Update Successfully";
            return RedirectToAction("Index");
        }
        catch
        {
            TempData[SweetAlert.Danger] = "An error occurred. Please try again.";
            return RedirectToAction("Index");
        }
    }

    [HttpPost]
    public async Task<IActionResult> Delete(string[] ids)
    {
        if (!ModelState.IsValid || ids.Length == 0)
        {
            TempData[SweetAlert.Danger] = Common.InvalidRequestErrorMessage;
            return Request.RedirectToReferer();
        }

        await _db.RoomCategories
            .Where(rc => ids.Contains(rc.Id.ToString()))
            .ForEachAsync(rc => { rc.IsDeleted = 1; });

        await _db.Rooms
            .Where(r => ids.Contains(r.RoomCategoryId.ToString()))
            .ForEachAsync(r => { r.IsDeleted = 1; });
        await _db.SaveChangesAsync();

        TempData[SweetAlert.Success] = $"Total {ids.Length} Room Category Deleted Successfully";
        return Request.RedirectToReferer();
    }

    [HttpPost]
    public async Task<IActionResult> PermanentDelete(string[] ids)
    {
        if (!ModelState.IsValid || ids.Length == 0)
        {
            TempData[SweetAlert.Danger] = Common.InvalidRequestErrorMessage;
            return Request.RedirectToReferer();
        }

        await _db.RoomCategories
            .Where(rc => ids.Contains(rc.Id.ToString()))
            .ForEachAsync(rc =>
            {
                rc.Type = "PD:" + rc.Type + "_" + DateTime.Now;
                rc.IsDeleted = 2;
            });

        await _db.Rooms
            .Where(r => ids.Contains(r.RoomCategoryId.ToString()))
            .ForEachAsync(r =>
            {
                r.RoomCode = "PD:" + r.RoomCode + "_" + DateTime.Now;
                r.IsDeleted = 2;
            });

        await _db.SaveChangesAsync();

        TempData[SweetAlert.Success] = $"Total {ids.Length} Room Category Permanent Deleted Successfully";
        return Request.RedirectToReferer();
    }

    [HttpPost]
    public async Task<IActionResult> Recover(string[] ids)
    {
        if (!ModelState.IsValid || ids.Length == 0)
        {
            TempData[SweetAlert.Danger] = Common.InvalidRequestErrorMessage;
            return Request.RedirectToReferer();
        }

        await _db.RoomCategories
            .Where(r => ids.Contains(r.Id.ToString()))
            .ForEachAsync(r => { r.IsDeleted = 0; });

        await _db.Rooms
            .Where(r => ids.Contains(r.RoomCategoryId.ToString()))
            .ForEachAsync(r => { r.IsDeleted = 0; });

        await _db.SaveChangesAsync();

        TempData[SweetAlert.Success] = $"Total {ids.Length} Room Category Recovered Successfully";
        return Request.RedirectToReferer();
    }

    [HttpPost]
    public async Task<IActionResult> DeleteImage(int imageId)
    {
        if (!ModelState.IsValid)
        {
            TempData[SweetAlert.Danger] = Common.InvalidRequestErrorMessage;
            return Request.RedirectToReferer();
        }

        RoomImage? roomCategoryImage = await _db.RoomImages
            .FirstOrDefaultAsync(ri => ri.Id == imageId);

        if (roomCategoryImage == null)
        {
            TempData[SweetAlert.Danger] = "Room Category Image not found";
            return RedirectToAction("Index");
        }

        int totalImages = await _db.RoomImages
            .CountAsync(ri => ri.RoomCategoryId == roomCategoryImage.RoomCategoryId);

        if (totalImages <= 1)
        {
            TempData[SweetAlert.Danger] = "At least one image is required for this room category.";
            return Request.RedirectToReferer();
        }

        _imageService.DeletePhoto(roomCategoryImage.ImagePath, Common.ImageUploadPath);

        _db.RoomImages.Remove(roomCategoryImage);
        await _db.SaveChangesAsync();
        TempData[SweetAlert.Success] = "Remove successful";
        return Request.RedirectToReferer();
    }


    [HttpPost]
    public async Task<IActionResult> UpdateReviewStatus(string[] ids, string status)
    {
        if (!ModelState.IsValid || ids.Length == 0)
        {
            TempData[SweetAlert.Danger] = Common.InvalidRequestErrorMessage;
            return Request.RedirectToReferer();
        }

        try
        {
            if (!Enum.TryParse(status, out RoomCategoryReviewStatus rStatus)) throw new Exception("Invalid Status");

            List<RoomCategoryReview> roomCategoryReviews = await _db.RoomCategoryReviews
                .Where(rc => ids.Contains(rc.Id.ToString()))
                .ToListAsync();

            roomCategoryReviews.ForEach(rcr =>
            {
                rcr.Status = rStatus;
                rcr.UpdatedAt = DateTime.Now;
            });

            await _db.SaveChangesAsync();

            TempData[SweetAlert.Success] = $"Total {ids.Length} Room Category review(s) updated successfully.";
        }
        catch (Exception ex)
        {
            TempData[SweetAlert.Danger] = $"Error updating reviews: {ex.Message}";
        }

        return RedirectToAction("Review", "RoomCategory");
    }

    private IPagedList<RoomCategoryReview> SortAndPaginateRoomCategoryReview(List<RoomCategoryReview> searched,
        string? sort, string? dir, int page = 1)
    {
        ViewBag.Sort = sort;
        ViewBag.Dir = dir;

        Func<RoomCategoryReview, object> fn = sort switch
        {
            "Id" => s => s.Id,
            "UserName" => s => s.User.UserName,
            "RoomCategory" => s => s.RoomCategory.Type,
            "Message" => s => s.Message,
            "Rating" => s => s.Rating,
            "CreateAt" => s => s.CreatedAt,
            "UpdateAt" => s => s.UpdatedAt,
            _ => s => s.Id
        };

        IOrderedEnumerable<RoomCategoryReview> sorted = dir == "des"
            ? searched.AsEnumerable().OrderByDescending(fn)
            : searched.AsEnumerable().OrderBy(fn);

        // (3) Paging ---------------------------
        IPagedList<RoomCategoryReview> roomReviewPagedList = sorted.ToPagedList(page, 10);

        return roomReviewPagedList;
    }

    private IPagedList<RoomCategory> SortAndPaginateRoomCategory(List<RoomCategory> searched, string? sort,
        string? dir, int page = 1)
    {
        ViewBag.Sort = sort;
        ViewBag.Dir = dir;

        Func<RoomCategory, object> fn = sort switch
        {
            "Id" => s => s.Id,
            "Type" => s => s.Type,
            "Price(RM)" => s => s.Price,
            "Description" => s => s.Description,
            "Max People" => s => s.MaxPeople,
            "AddBed" => s => s.IsAddBed,
            "Create At" => s => s.CreatedAt,
            _ => s => s.Id
        };

        IOrderedEnumerable<RoomCategory> sorted = dir == "des"
            ? searched.AsEnumerable().OrderByDescending(fn)
            : searched.AsEnumerable().OrderBy(fn);

        // (3) Paging ---------------------------
        IPagedList<RoomCategory> roomPagedList = sorted.ToPagedList(page, 10);

        return roomPagedList;
    }
}
