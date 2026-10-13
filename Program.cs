global using static HotelReservationSystem.Helper;
global using System.ComponentModel;
global using Microsoft.AspNetCore.Authentication.Cookies;
global using Microsoft.AspNetCore.HttpOverrides;
global using Microsoft.EntityFrameworkCore;

namespace HotelReservationSystem;

/**
 * @description: Program Setup by Chong Jun Xiang
 */
public class Program
{
    public static void Main(string[] args)
    {
        // Step 1: Create the builder
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        // Step 2: WebApplication.CreateBuilder already loads appsettings.json,
        // appsettings.{Environment}.json, and environment variables in the
        // correct precedence order. Do not reload JSON here because it would
        // override production environment variables such as the DB connection.

        // Step 3: Configure database context. In production, set
        // ConnectionStrings__DBConnection as an environment variable.
        string connectionString = builder.Configuration.GetConnectionString("DBConnection") ??
                                  throw new InvalidOperationException(
                                      "Connection string 'DBConnection' not found.");
        builder.Services.AddDbContext<DB>(options =>
            options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure()));

        // Railway supplies the public port through the PORT environment variable.
        string? port = Environment.GetEnvironmentVariable("PORT");
        if (!string.IsNullOrWhiteSpace(port))
        {
            builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
        }

        // Step 4: Configure authentication and authorization
        builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie("Cookies", options =>
            {
                options.LoginPath = "/Login";
                options.LogoutPath = "/Account/Logout";
                options.AccessDeniedPath = "/Home/AccessDenied";
                options.Cookie.Name = "logged_in";
                options.SlidingExpiration = true;
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            });

        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy(AuthorizationPolicies.SuperAdminOnly, policy =>
                policy.RequireRole(UserRoles.SuperAdmin));

            options.AddPolicy(AuthorizationPolicies.RequireAdminOrHigher, policy =>
                policy.RequireRole(UserRoles.Admin, UserRoles.SuperAdmin));

            options.AddPolicy(AuthorizationPolicies.AuthenticatedUsersOnly, policy =>
                policy.RequireAuthenticatedUser());

            options.AddPolicy(AuthorizationPolicies.UserOnly, policy =>
                policy.RequireRole(UserRoles.User));
        });

        // Step 5: Configure session
        builder.Services.AddSession(options =>
        {
            options.IdleTimeout = TimeSpan.FromMinutes(30);
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
        });

        // Step 6: Configure CORS
        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
        });

        // Step 7: Bind configuration sections
        ConfigurationManager configuration = builder.Configuration;
        ConfigureAndRegister<Smtp>(builder.Services, configuration, "Smtp");
        ConfigureAndRegister<IpInfo>(builder.Services, configuration, "IpInfo");
        ConfigureAndRegister<StripeApp>(builder.Services, configuration, "Stripe");

        // Step 8: Add application services

        // Singleton services

        // Scoped services
        builder.Services.AddScoped<IUserClaimsSyncService, UserClaimsSyncService>();
        builder.Services.AddScoped<ILoginService, LoginService>();
        builder.Services.AddScoped<IResetMfaService, ResetMfaService>();
        builder.Services.AddScoped<IResetPasswordService, ResetPasswordService>();
        builder.Services.AddScoped<IStripeService, StripeService>();
        builder.Services.AddScoped<IPaymentService, PaymentService>();
        builder.Services.AddScoped<IRoomCategoryService, RoomCategoryService>();
        builder.Services.AddScoped<IRoomService, RoomService>();
        builder.Services.AddScoped<ITransactionService, TransactionService>();
        builder.Services.AddScoped<IOrderService, OrderService>();
        builder.Services.AddScoped<IReceiptService, ReceiptService>();
        builder.Services.AddScoped<IBookingService, BookingService>();
        builder.Services.AddScoped<IEmailService, EmailService>();
        builder.Services.AddScoped<IImageService, ImageService>();
        builder.Services.AddScoped<Helper>();

        // Transient services
        builder.Services.AddTransient<IMailerService, MailerService>();
        builder.Services.AddTransient<IIpInfoService, IpInfoService>();
        builder.Services.AddTransient<IGoogleMfaService, GoogleMfaService>();

        // Step 9: Add essential services and middleware
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddControllersWithViews();
        builder.Services.AddRazorTemplating();

        // Build the application
        WebApplication app = builder.Build();

        // Step 10: Configure middleware pipeline
        app.UseForwardedHeaders(); // Read the original HTTPS scheme behind a reverse proxy such as Render
        app.UseHttpsRedirection(); // Enforce HTTPS
        app.UseStaticFiles(); // Serve static files
        app.UseRouting(); // Enable routing
        app.UseCors(corsPolicyBuilder =>
            corsPolicyBuilder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()); // Configure CORS
        app.UseRequestLocalization("en-MY"); // Localization middleware
        app.UseAuthentication(); // Authentication middleware
        app.UseAuthorization(); // Authorization middleware
        app.UseSession(); // Session middleware
        // Custom middleware
        app.UseMiddleware<UserClaimsSyncMiddleware>();
        app.UseMiddleware<SyncMiddleware>();

        // Custom middleware to handle 405 Method Not Allowed
        app.Use(async (context, next) =>
        {
            await next();
            switch (context.Response.StatusCode)
            {
                case StatusCodes.Status405MethodNotAllowed:
                    context.Response.Redirect("/");
                    break;
            }
        });

        // Adding endpoints to the request pipeline
        app.UseEndpoints(endpoints => endpoints.MapControllers());

        // Step 11: Configure routing
        // Area routing
        app.MapControllerRoute(
            "areas",
            "{area:exists}/{controller=Home}/{action=Index}/{id?}");

        // Default route (no area)
        app.MapControllerRoute(
            "default",
            "{controller=Home}/{action=Index}/{id?}");

        // Step 12: Run the application
        app.Run();
    }

    private static T ConfigureAndRegister<T>(IServiceCollection services, IConfiguration configuration,
        string sectionName) where T : class, new()
    {
        T? configValue = configuration.GetSection(sectionName).Get<T>();
        services.AddSingleton(configValue);
        return configValue;
    }
}
