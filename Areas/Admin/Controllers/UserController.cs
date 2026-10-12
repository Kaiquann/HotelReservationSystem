using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HotelReservationSystem.Areas.Admin.Controllers;

[Authorize(Policy = AuthorizationPolicies.RequireAdminOrHigher)]
[Area("Admin")]
public class UserController : Controller
{
    private readonly DB _db;
    private readonly IEmailService _emailService;
    private readonly IImageService _imageService;
    private readonly IMailerService _mailerService;
    private readonly IResetPasswordService _resetPasswordService;

    public UserController(DB db, IEmailService emailService, IMailerService mailerService, IImageService imageService,
        IResetPasswordService resetPasswordService)
    {
        _db = db;
        _emailService = emailService;
        _mailerService = mailerService;
        _imageService = imageService;
        _resetPasswordService = resetPasswordService;
    }

    public async Task<IActionResult> Index(string? search, string? sort, string? dir, string? filter, int page = 1)
    {
        User? currentUser = await User.GetCurrentUser(_db);
        if (currentUser == null)
        {
            TempData[SweetAlert.Danger] = Common.PleaseLoginToContinueErrorMessage;
            return RedirectToAction("Index", "Home");
        }

        List<Role> roles = await _db.GetRolesAsync(currentUser);
        SelectList roleSelectList = new(roles, "Name", "Name", sort);
        ViewBag.Roles = roleSelectList;
        ViewBag.Search = search = search?.Trim() ?? "";
        ViewBag.Filter = filter = filter?.Trim() ?? "";

        List<User> searched = await _db.Users
            .Where(u => (u.FirstName.Contains(search) ||
                         u.LastName.Contains(search) ||
                         u.Email.Contains(search) ||
                         u.PhoneNumber.Contains(search)) &&
                        (filter == "" || u.Role == filter) &&
                        // Exclude SuperAdmin and Current Logged In User
                        !u.IsBlocked &&
                        u.IsDeleted == 0 &&
                        u.Role != UserRoles.SuperAdmin &&
                        u.Id != currentUser.Id)
            .ToListAsync();

        if (page < 1) return RedirectToAction(null, new { search, sort, dir, filter, page = 1 });

        IPagedList<User> model = SortAndPaginateUsers(searched, sort, dir, page);

        if (page > model.PageCount && model.PageCount > 0)
            return RedirectToAction(null, new { search, sort, dir, filter, page = model.PageCount });

        if (Request.IsAjax()) return PartialView("_Index", model);

        return View(model);
    }

    public async Task<IActionResult> Blocked(string? search, string? sort, string? dir, string? filter, int page = 1)
    {
        User? currentUser = await User.GetCurrentUser(_db);
        if (currentUser == null)
        {
            TempData[SweetAlert.Danger] = Common.PleaseLoginToContinueErrorMessage;
            return RedirectToAction("Index", "Home");
        }

        List<Role> roles = await _db.GetRolesAsync(currentUser);
        SelectList roleSelectList = new(roles, "Name", "Name", sort);
        ViewBag.Roles = roleSelectList;
        ViewBag.Search = search = search?.Trim() ?? "";
        ViewBag.Filter = filter = filter?.Trim() ?? "";

        List<User> searched = await _db.Users
            .Where(u => (u.FirstName.Contains(search) ||
                         u.LastName.Contains(search) ||
                         u.Email.Contains(search) ||
                         u.PhoneNumber.Contains(search)) &&
                        (filter == "" || u.Role == filter) &&
                        // Exclude SuperAdmin and Current Logged In User
                        u.IsBlocked &&
                        u.IsDeleted == 0 &&
                        u.Role != UserRoles.SuperAdmin &&
                        u.Id != currentUser.Id)
            .ToListAsync();

        if (page < 1) return RedirectToAction(null, new { search, sort, dir, filter, page = 1 });

        IPagedList<User> model = SortAndPaginateUsers(searched, sort, dir, page);

        if (page > model.PageCount && model.PageCount > 0)
            return RedirectToAction(null, new { search, sort, dir, filter, page = model.PageCount });

        if (Request.IsAjax()) return PartialView("_Index", model);

        return View(model);
    }

    public async Task<IActionResult> Deleted(string? search, string? sort, string? dir, string? filter, int page = 1)
    {
        User? currentUser = await User.GetCurrentUser(_db);
        if (currentUser == null)
        {
            TempData[SweetAlert.Danger] = Common.PleaseLoginToContinueErrorMessage;
            return RedirectToAction("Index", "Home");
        }

        List<Role> roles = await _db.GetRolesAsync(currentUser);
        SelectList roleSelectList = new(roles, "Name", "Name", sort);
        ViewBag.Roles = roleSelectList;
        ViewBag.Search = search = search?.Trim() ?? "";
        ViewBag.Filter = filter = filter?.Trim() ?? "";

        List<User> searched = await _db.Users
            .Where(u => (u.FirstName.Contains(search) ||
                         u.LastName.Contains(search) ||
                         u.Email.Contains(search) ||
                         u.PhoneNumber.Contains(search)) &&
                        (filter == "" || u.Role == filter) &&
                        // Exclude SuperAdmin and Current Logged In User
                        u.IsDeleted == 1 &&
                        u.Role != UserRoles.SuperAdmin &&
                        u.Id != currentUser.Id)
            .ToListAsync();

        if (page < 1) return RedirectToAction(null, new { search, sort, dir, filter, page = 1 });

        IPagedList<User> model = SortAndPaginateUsers(searched, sort, dir, page);

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

        if (currentUser.Id.ToString() == id)
        {
            TempData[SweetAlert.Danger] = "You Cannot View Your Own Account";
            return RedirectToAction("Index");
        }

        // Find the pass in user id
        User? user = await FindUserAsync(id);
        if (user == null)
        {
            TempData[SweetAlert.Danger] = "User Not Found";
            return RedirectToAction("Index");
        }

        if (Request.IsAjax()) return PartialView("_Show", user);

        return View(user);
    }

    public async Task<IActionResult> Create()
    {
        User? currentUser = await User.GetCurrentUser(_db);
        if (currentUser == null)
        {
            TempData[SweetAlert.Danger] = Common.PleaseLoginToContinueErrorMessage;
            return RedirectToAction("Index", "Home");
        }

        List<Role> roles = await _db.GetRolesAsync(currentUser);
        SelectList roleSelectList = new(roles, "Name", "Name");

        CreateUserVm createUserVm = new()
        {
            Roles = roleSelectList
        };

        return View(createUserVm);
    }

    [HttpPost]
    public async Task<IActionResult> Store(CreateUserVm createUserVm)
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

        // validate the email
        bool isEmailExists = await _db.IsEmailExists(createUserVm.Email);
        if (isEmailExists)
        {
            TempData[SweetAlert.Danger] = Common.EmailExistsErrorMessage;
            return Request.RedirectToReferer();
        }

        // validate the roles
        bool isValidRole = await _db.IsValidRole(createUserVm.Role);
        if (!isValidRole)
        {
            TempData[SweetAlert.Danger] = "Invalid Role";
            return Request.RedirectToReferer();
        }

        string password = GetRandomString(16);

        // create the user
        User newUser = new()
        {
            FirstName = createUserVm.FirstName,
            LastName = createUserVm.LastName,
            UserName = createUserVm.Email,
            Email = createUserVm.Email,
            Password = HashPassword(password),
            Role = createUserVm.Role
        };

        await _db.Users.AddAsync(newUser);
        await _db.SaveChangesAsync();

        string? url = Url.Action("Index", "Login", new { area = "" }, Request.Scheme);

        AccountCreatedByAdminVm accountCreatedByAdminVm = new()
        {
            FirstName = newUser.FirstName,
            LastName = newUser.LastName,
            Email = newUser.Email,
            Password = password,
            Url = url!
        };
        string body = await _emailService.AccountCreatedByAdmin(accountCreatedByAdminVm);

        Mail mail = new()
        {
            To = [newUser.Email],
            Subject = EmailDesign.AccountCreatedSubject,
            Body = body
        };
        await _mailerService.SendEmail(mail);

        TempData[SweetAlert.Success] = "User Created Successfully";
        return RedirectToAction("Show", new { id = newUser.Id });
    }

    public async Task<IActionResult> Edit(string id)
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

        if (currentUser.Id.ToString() == id)
        {
            TempData[SweetAlert.Danger] = "You Cannot Edit Your Own Account";
            return Request.RedirectToReferer();
        }

        // Find the pass in user id
        User? user = await FindUserAsync(id);
        if (user == null)
        {
            TempData[SweetAlert.Danger] = "User Not Found";
            return RedirectToAction("Index");
        }

        if (currentUser.Role == UserRoles.Admin &&
            await _db.Users.AnyAsync(u => u.Id == user.Id && u.Role == UserRoles.Admin))
        {
            TempData[SweetAlert.Danger] = "You Cannot Edit Admin Account";
            return Request.RedirectToReferer();
        }

        List<Role> roles = await _db.GetRolesAsync(currentUser);
        SelectList roleSelectList = new(roles, "Name", "Name", user.Role);

        EditUserVm editUserVm = new()
        {
            Id = user.Id.ToString(),
            FirstName = user.FirstName,
            LastName = user.LastName,
            UserName = user.UserName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            Role = user.Role,
            ProfileImageUrl = user.ProfileImageUrl,
            IsBlocked = user.IsBlocked,
            IsDeleted = user.IsDeleted,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            Roles = roleSelectList
        };

        if (Request.IsAjax()) return PartialView("_Edit", editUserVm);

        return View(editUserVm);
    }

    [HttpPost]
    public async Task<IActionResult> Update(EditUserVm editUserVm)
    {
        try
        {
            User? currentUser = await User.GetCurrentUser(_db);
            if (currentUser == null) throw new Exception(Common.PleaseLoginToContinueErrorMessage);

            if (!ModelState.IsValid) throw new Exception(Common.InvalidRequestErrorMessage);

            if (currentUser.Id.ToString() == editUserVm.Id) throw new Exception("You cannot update your own account!");

            // Find the pass in user id
            User? user = await FindUserAsync(editUserVm.Id);
            if (user == null) throw new Exception("User not found!");

            if (currentUser.Role == UserRoles.Admin &&
                await _db.Users.AnyAsync(u => u.Id == user.Id && u.Role == UserRoles.Admin))
                throw new Exception("You cannot update admin account!");

            // validate the roles
            bool isValidRole = await _db.IsValidRole(editUserVm.Role);
            if (!isValidRole) throw new Exception("Invalid role!");

            if (user.Email != editUserVm.Email)
            {
                bool isEmailExists = await _db.IsEmailExists(editUserVm.Email);
                if (isEmailExists) throw new Exception(Common.EmailExistsErrorMessage);
            }

            if (editUserVm.ProfileImage != null)
            {
                string error = _imageService.ValidatePhoto(editUserVm.ProfileImage);
                if (error != string.Empty) ModelState.AddModelError("ProfileImage", error);
            }

            if (editUserVm.ProfileImage != null)
            {
                if (!string.IsNullOrEmpty(user.ProfileImageUrl))
                    try
                    {
                        _imageService.DeletePhoto(user.ProfileImageUrl, Common.ImageUploadPath);
                    }
                    catch
                    {
                        // ignored
                    }

                try
                {
                    user.ProfileImageUrl = _imageService.SavePhoto(editUserVm.ProfileImage, Common.ImageUploadPath);
                }
                catch
                {
                    throw new Exception("Failed to upload image");
                }
            }

            user.FirstName = editUserVm.FirstName;
            user.LastName = editUserVm.LastName;
            user.UserName = editUserVm.UserName;
            user.Email = editUserVm.Email;
            user.PhoneNumber = editUserVm.PhoneNumber ?? string.Empty;
            user.Role = editUserVm.Role;
            user.UpdatedAt = DateTime.Now;

            await _db.SaveChangesAsync();

            TempData[SweetAlert.Success] = "User updated successfully";
        }
        catch (Exception ex)
        {
            TempData[SweetAlert.Danger] = ex.Message;
        }

        return Request.RedirectToReferer();
    }

    [HttpPost]
    public async Task<IActionResult> RemoveProfileImage(string id)
    {
        try
        {
            User? currentUser = await User.GetCurrentUser(_db);
            if (currentUser == null) throw new Exception(Common.PleaseLoginToContinueErrorMessage);

            if (!ModelState.IsValid) throw new Exception(Common.InvalidRequestErrorMessage);

            if (currentUser.Id.ToString() == id) throw new Exception("You cannot remove your own profile image");

            // Find the pass in user id
            User? user = await FindUserAsync(id);
            if (user == null) throw new Exception("User not found!");

            if (string.IsNullOrEmpty(user.ProfileImageUrl)) throw new Exception("No image to remove!");

            if (!string.IsNullOrEmpty(user.ProfileImageUrl))
                try
                {
                    _imageService.DeletePhoto(user.ProfileImageUrl, Common.ImageUploadPath);
                }
                catch
                {
                    // ignored
                }

            user.ProfileImageUrl = null;
            user.UpdatedAt = DateTime.Now;

            await _db.SaveChangesAsync();

            TempData[SweetAlert.Success] = "User profile image removed successfully";
        }
        catch (Exception ex)
        {
            TempData[SweetAlert.Danger] = ex.Message;
        }

        return Request.RedirectToReferer();
    }

    [HttpPost]
    public async Task<IActionResult> SendResetPasswordLink(string id)
    {
        try
        {
            User? currentUser = await User.GetCurrentUser(_db);
            if (currentUser == null) throw new Exception(Common.PleaseLoginToContinueErrorMessage);

            if (!ModelState.IsValid) throw new Exception(Common.InvalidRequestErrorMessage);

            if (currentUser.Id.ToString() == id) throw new Exception("You cannot reset your own password!");

            User? user = await FindUserAsync(id);
            if (user == null) throw new Exception("User not found!");

            string token = Guid.NewGuid().ToString();

            ResetPassword? resetPassword =
                await _resetPasswordService.GetResetPasswordByUserIdAsync(user.Id.ToString());
            if (resetPassword != null)
            {
                resetPassword.Token = token;
            }
            else
            {
                resetPassword = new ResetPassword
                {
                    UserId = user.Id,
                    Token = token
                };
                await _db.ResetPasswords.AddAsync(resetPassword);
            }

            await _db.SaveChangesAsync();

            string? url = Url.Action("", "ResetPassword", new { area = "", token }, Request.Scheme);
            ResetPasswordVm resetPasswordVm = new()
            {
                Url = url!
            };
            string body = await _emailService.ResetPassword(resetPasswordVm);

            Mail mail = new()
            {
                To = [user.Email],
                Subject = EmailDesign.ResetPasswordSubject,
                Body = body
            };
            await _mailerService.SendEmail(mail);

            TempData[SweetAlert.Success] = "Reset password link sent successfully";
        }
        catch (Exception ex)
        {
            TempData[SweetAlert.Danger] = ex.Message;
        }

        return Request.RedirectToReferer();
    }

    /*
     * Support Single And Bulk Actions
     * @description: Block, Delete, Unblock, Recover, PermanentDelete
     */
    [HttpPost]
    public async Task<IActionResult> Block(string[] ids)
    {
        try
        {
            User? currentUser = await User.GetCurrentUser(_db);
            if (currentUser == null) throw new Exception(Common.PleaseLoginToContinueErrorMessage);

            if (!ModelState.IsValid || ids.Length == 0) throw new Exception(Common.InvalidRequestErrorMessage);

            if (ids.Contains(currentUser.Id.ToString())) throw new Exception("You cannot block your own account!");

            if (currentUser.Role == UserRoles.Admin &&
                await _db.Users.AnyAsync(u => ids.Contains(u.Id.ToString()) && u.Role == UserRoles.Admin))
                throw new Exception("You cannot block admin account!");

            string subject = "Account Blocked";
            string message = "Your account has been blocked by admin on date " + DateTime.Now;
            await SendEmail(ids, subject, message);

            await _db.Users
                .Where(u => ids.Contains(u.Id.ToString()))
                .ForEachAsync(u =>
                {
                    u.IsBlocked = true;
                    u.UpdatedAt = DateTime.Now;
                });

            await _db.SaveChangesAsync();

            TempData[SweetAlert.Success] = $"Total {ids.Length} user blocked successfully";
        }
        catch (Exception ex)
        {
            TempData[SweetAlert.Danger] = ex.Message;
        }

        return Request.RedirectToReferer();
    }

    [HttpPost]
    public async Task<IActionResult> Delete(string[] ids)
    {
        try
        {
            User? currentUser = await User.GetCurrentUser(_db);
            if (currentUser == null) throw new Exception(Common.PleaseLoginToContinueErrorMessage);

            if (!ModelState.IsValid || ids.Length == 0) throw new Exception(Common.InvalidRequestErrorMessage);

            if (ids.Contains(currentUser.Id.ToString())) throw new Exception("You cannot delete your own account!");

            if (currentUser.Role == UserRoles.Admin &&
                await _db.Users.AnyAsync(u => ids.Contains(u.Id.ToString()) && u.Role == UserRoles.Admin))
                throw new Exception("You cannot delete admin account!");

            string subject = "Account Deleted";
            string message = "Your account has been deleted by admin on date " + DateTime.Now;
            await SendEmail(ids, subject, message);

            await _db.Users
                .Where(u => ids.Contains(u.Id.ToString()))
                .ForEachAsync(u =>
                {
                    u.IsDeleted = 1;
                    u.IsBlocked = false;
                    u.UpdatedAt = DateTime.Now;
                });

            await _db.SaveChangesAsync();

            TempData[SweetAlert.Success] = $"Total {ids.Length} user deleted successfully";
        }
        catch (Exception ex)
        {
            TempData[SweetAlert.Danger] = ex.Message;
        }

        return Request.RedirectToReferer();
    }

    [HttpPost]
    public async Task<IActionResult> Unblock(string[] ids)
    {
        try
        {
            User? currentUser = await User.GetCurrentUser(_db);
            if (currentUser == null) throw new Exception(Common.PleaseLoginToContinueErrorMessage);

            if (!ModelState.IsValid || ids.Length == 0) throw new Exception(Common.InvalidRequestErrorMessage);

            if (ids.Contains(currentUser.Id.ToString())) throw new Exception("You cannot unblock your own account!");

            if (currentUser.Role == UserRoles.Admin &&
                await _db.Users.AnyAsync(u => ids.Contains(u.Id.ToString()) && u.Role == UserRoles.Admin))
                throw new Exception("You cannot unblock admin account!");

            string subject = "Account Unblocked";
            string message = "Your account has been unblocked by admin on date " + DateTime.Now;
            await SendEmail(ids, subject, message);

            await _db.Users
                .Where(u => ids.Contains(u.Id.ToString()))
                .ForEachAsync(u =>
                {
                    u.IsBlocked = false;
                    u.UpdatedAt = DateTime.Now;
                });

            await _db.SaveChangesAsync();

            TempData[SweetAlert.Success] = $"Total {ids.Length} user unblocked successfully!";
            return Request.RedirectToReferer();
        }
        catch (Exception ex)
        {
            TempData[SweetAlert.Danger] = ex.Message;
        }

        return Request.RedirectToReferer();
    }

    [HttpPost]
    public async Task<IActionResult> Recover(string[] ids)
    {
        try
        {
            User? currentUser = await User.GetCurrentUser(_db);
            if (currentUser == null) throw new Exception(Common.PleaseLoginToContinueErrorMessage);

            if (!ModelState.IsValid || ids.Length == 0) throw new Exception(Common.InvalidRequestErrorMessage);

            if (ids.Contains(currentUser.Id.ToString())) throw new Exception("You cannot recover your own account!");

            if (currentUser.Role == UserRoles.Admin &&
                await _db.Users.AnyAsync(u => ids.Contains(u.Id.ToString()) && u.Role == UserRoles.Admin))
                throw new Exception("You cannot recover admin account!");

            string subject = "Account Recovered";
            string message = "Your account has been recovered by admin on date " + DateTime.Now;
            await SendEmail(ids, subject, message);

            await _db.Users
                .Where(u => ids.Contains(u.Id.ToString()))
                .ForEachAsync(u =>
                {
                    u.IsDeleted = 0;
                    u.IsBlocked = false;
                    u.UpdatedAt = DateTime.Now;
                });

            await _db.SaveChangesAsync();

            TempData[SweetAlert.Success] = $"Total {ids.Length} user recovered successfully!";
        }
        catch (Exception ex)
        {
            TempData[SweetAlert.Danger] = ex.Message;
        }

        return Request.RedirectToReferer();
    }

    [HttpPost]
    public async Task<IActionResult> PermanentDelete(string[] ids)
    {
        try
        {
            User? currentUser = await User.GetCurrentUser(_db);
            if (currentUser == null) throw new Exception(Common.PleaseLoginToContinueErrorMessage);

            if (!ModelState.IsValid || ids.Length == 0) throw new Exception(Common.InvalidRequestErrorMessage);

            if (ids.Contains(currentUser.Id.ToString()))
                throw new Exception("You cannot permanent delete your own account!");

            if (currentUser.Role == UserRoles.Admin &&
                await _db.Users.AnyAsync(u => ids.Contains(u.Id.ToString()) && u.Role == UserRoles.Admin))
                throw new Exception("You cannot permanent admin account!");

            string subject = "Account Permanent Deleted";
            string message = "Your account has been permanent deleted by admin on date " + DateTime.Now;
            await SendEmail(ids, subject, message);

            List<string> userImageList = [];

            await _db.Users
                .Where(u => ids.Contains(u.Id.ToString()))
                .ForEachAsync(u =>
                {
                    u.Email = "PD:" + u.Email + "_" + DateTime.Now;
                    u.IsDeleted = 2;
                    u.IsBlocked = false;
                    u.UpdatedAt = DateTime.Now;
                    if (!string.IsNullOrEmpty(u.ProfileImageUrl))
                    {
                        userImageList.Add(u.ProfileImageUrl);
                        u.ProfileImageUrl = null;
                    }
                });

            await _db.SaveChangesAsync();

            foreach (string userImage in userImageList)
                try
                {
                    _imageService.DeletePhoto(userImage, Common.ImageUploadPath);
                }
                catch
                {
                    // ignored
                }

            TempData[SweetAlert.Success] = $"Total {ids.Length} user permanent deleted successfully!";
        }
        catch (Exception ex)
        {
            TempData[SweetAlert.Danger] = ex.Message;
        }

        return Request.RedirectToReferer();
    }

    [HttpPost]
    public async Task<IActionResult> UpdateRole(string[] ids, string role)
    {
        try
        {
            User? currentUser = await User.GetCurrentUser(_db);
            if (currentUser == null) throw new Exception(Common.PleaseLoginToContinueErrorMessage);

            if (!ModelState.IsValid || ids.Length == 0) throw new Exception(Common.InvalidRequestErrorMessage);

            if (!await _db.IsValidRole(role)) throw new Exception("Invalid role!");

            if (ids.Contains(currentUser.Id.ToString())) throw new Exception("You cannot update your account roles!");

            if (currentUser.Role == UserRoles.Admin &&
                await _db.Users.AnyAsync(u => ids.Contains(u.Id.ToString()) && u.Role == UserRoles.Admin))
                throw new Exception("You cannot update admin account roles!");

            await _db.Users
                .Where(u => ids.Contains(u.Id.ToString()))
                .ForEachAsync(u =>
                {
                    u.Role = role;
                    u.UpdatedAt = DateTime.Now;
                });

            await _db.SaveChangesAsync();

            TempData[SweetAlert.Success] = $"Total {ids.Length} user role updated successfully!";
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
    private IPagedList<User> SortAndPaginateUsers(List<User> searched, string? sort, string? dir, int page = 1)
    {
        // (2) Sorting --------------------------
        ViewBag.Sort = sort;
        ViewBag.Dir = dir;

        Func<User, object> fn = sort switch
        {
            "FirstName" => s => s.FirstName,
            "LastName" => s => s.LastName,
            "Email" => s => s.Email,
            "PhoneNumber" => s => s.PhoneNumber,
            "Role" => s => s.Role,
            "CreatedAt" => s => s.CreatedAt,
            _ => s => s.Id
        };

        IOrderedEnumerable<User> sorted = dir == "des"
            ? searched.AsEnumerable().OrderByDescending(fn)
            : searched.AsEnumerable().OrderBy(fn);

        // (3) Paging ---------------------------
        IPagedList<User> userPagedList = sorted.ToPagedList(page, 10);

        return userPagedList;
    }

    private async Task SendEmail(string[] ids, string subject, string? message)
    {
        string[] emailList = await _db.Users
            .Where(u => ids.Contains(u.Id.ToString()))
            .Select(u => u.Email)
            .ToArrayAsync();

        SimpleTitleMessageVm simpleTitleMessageVm = new()
        {
            Title = subject,
            Message = message!
        };
        string body = await _emailService.SimpleTitleMessage(simpleTitleMessageVm);

        Mail mail = new()
        {
            Bcc = emailList,
            Subject = subject,
            Body = body
        };
        await _mailerService.SendEmail(mail);
    }

    private async Task<User?> FindUserAsync(string id)
    {
        return await _db.Users.SingleOrDefaultAsync(u => u.Id.ToString() == id &&
                                                         u.IsDeleted < 2 &&
                                                         u.Role != UserRoles.SuperAdmin);
    }
}