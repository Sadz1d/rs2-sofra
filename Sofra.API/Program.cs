using System.Text;
using DotNetEnv;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Sofra.API.Data;
using Sofra.API.DTOs;
using Sofra.API.Entities;
using Sofra.API.Filters;
using Sofra.API.Hubs;
using Sofra.API.Identity;
using Sofra.API.Messaging;
using Sofra.API.Messaging.Consumers;
using Sofra.API.Middleware;
using Sofra.API.Options;
using Sofra.API.Services;
using Sofra.API.Services.Interfaces;

try
{
    Env.TraversePath().Load();
}
catch (FileNotFoundException)
{
    // U Docker kontejneru .env fajl ne postoji — varijable su već postavljene kroz docker-compose (env_file/environment).
}

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, loggerConfiguration) => loggerConfiguration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/sofra-api-.log", rollingInterval: RollingInterval.Day));

builder.Services.AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddOptions<RabbitMqOptions>()
    .Bind(builder.Configuration.GetSection(RabbitMqOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddOptions<SmtpOptions>()
    .Bind(builder.Configuration.GetSection(SmtpOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddOptions<StripeOptions>()
    .Bind(builder.Configuration.GetSection(StripeOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddOptions<RecommenderOptions>()
    .Bind(builder.Configuration.GetSection(RecommenderOptions.SectionName));
builder.Services.AddOptions<CorsOptions>()
    .Bind(builder.Configuration.GetSection(CorsOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddOptions<SeedOptions>()
    .Bind(builder.Configuration.GetSection(SeedOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddOptions<ApiOptions>()
    .Bind(builder.Configuration.GetSection(ApiOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddOptions<OrderOptions>()
    .Bind(builder.Configuration.GetSection(OrderOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddOptions<RestaurantOptions>()
    .Bind(builder.Configuration.GetSection(RestaurantOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddOptions<ReservationOptions>()
    .Bind(builder.Configuration.GetSection(ReservationOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

const string CorsPolicyName = "SofraCors";
var corsAllowedOrigins = builder.Configuration.GetValue<string>($"{CorsOptions.SectionName}:AllowedOrigins") ?? string.Empty;
builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicyName, policy => policy
        .WithOrigins(corsAllowedOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddMemoryCache();

builder.Services
    .AddIdentityCore<ApplicationUser>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireDigit = true;
    })
    .AddErrorDescriber<BosnianIdentityErrorDescriber>()
    .AddRoles<IdentityRole<int>>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

var stripeOptions = builder.Configuration.GetSection(StripeOptions.SectionName).Get<StripeOptions>()!;
Stripe.StripeConfiguration.ApiKey = stripeOptions.SecretKey;

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()!;
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
            ClockSkew = TimeSpan.Zero,
        };
        options.Events = new JwtBearerEvents
        {
            // WebSocket ne nosi Authorization header - SignalR JS klijent zato salje token kroz
            // access_token query string na /hubs/* putanje; svugdje drugo i dalje samo Bearer header.
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken) && context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                var denylist = context.HttpContext.RequestServices.GetRequiredService<IJtiDenylistService>();
                var jti = context.Principal?.Claims.FirstOrDefault(c => c.Type == System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti)?.Value;
                if (jti is not null && denylist.IsDenied(jti))
                {
                    context.Fail("Token je opozvan.");
                }

                return Task.CompletedTask;
            },
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddSingleton<IJtiDenylistService, JtiDenylistService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<Sofra.API.Data.Seed.DataSeeder>();
builder.Services.AddScoped<IMenuCategoryService, MenuCategoryService>();
builder.Services.AddScoped<IAllergenService, AllergenService>();
builder.Services.AddScoped<IDietaryTagService, DietaryTagService>();
builder.Services.AddScoped<ITableTypeService, TableTypeService>();
builder.Services.AddScoped<IInventoryCategoryService, InventoryCategoryService>();
builder.Services.AddScoped<IZoneService, ZoneService>();
builder.Services.AddScoped<IPaymentMethodService, PaymentMethodService>();
builder.Services.AddScoped<IUnitOfMeasureService, UnitOfMeasureService>();
builder.Services.AddScoped<ICountryService, CountryService>();
builder.Services.AddScoped<ICityService, CityService>();
builder.Services.AddScoped<IMenuItemService, MenuItemService>();
builder.Services.AddScoped<IImageUploadService, ImageUploadService>();
builder.Services.AddScoped<IDiningTableService, DiningTableService>();
builder.Services.AddScoped<IInventoryItemService, InventoryItemService>();
builder.Services.AddScoped<IOrderStateMachine, OrderStateMachine>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IDiningTableStatusService, DiningTableStatusService>();
builder.Services.AddScoped<IReservationStateMachine, ReservationStateMachine>();
builder.Services.AddScoped<IReservationService, ReservationService>();

builder.Services.AddSingleton<RabbitMqConnectionService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<RabbitMqConnectionService>());
builder.Services.AddScoped<IEventPublisher, EventPublisher>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<IUserService, UserService>();

builder.Services.AddHostedService<OrderStatusChangedNotifyConsumer>();
builder.Services.AddHostedService<ReservationProcessedNotifyConsumer>();
builder.Services.AddHostedService<LowStockDetectedNotifyConsumer>();
builder.Services.AddHostedService<PaymentSucceededNotifyConsumer>();
builder.Services.AddHostedService<TableStatusRecalculationHostedService>();

builder.Services.AddSignalR();

builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.Services.AddControllers(options => options.Filters.Add<ValidationFilter>());

// [ApiController] automatski vraca RFC7807 ValidationProblemDetails na 400 kad model binding
// (npr. neispravan JSON ili pogresan tip polja) padne prije nego ValidationFilter uopste dobije priliku.
// Ovdje ga preusmjeravamo na isti ErrorResponse oblik (422) koji koristi ValidationException,
// da Flutter forme uvijek dobiju identičnu strukturu bez obzira gdje je validacija pukla.
builder.Services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(entry => entry.Value is { Errors.Count: > 0 })
            .ToDictionary(
                entry => entry.Key,
                entry => entry.Value!.Errors.Select(e => e.ErrorMessage).ToArray());

        var response = new ErrorResponse("Jedan ili više unesenih podataka nisu ispravni.", errors);
        return new Microsoft.AspNetCore.Mvc.UnprocessableEntityObjectResult(response);
    };
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
    {
        Title = "Sofra API",
        Version = "v1",
    });

    options.AddSecurityDefinition("bearer", new Microsoft.OpenApi.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.ParameterLocation.Header,
        Description = "Unesite JWT access token (bez 'Bearer ' prefiksa).",
    });
    // IOperationFilter ne radi ovdje: OperationFilterContext nema pristup OpenApiDocument-u koji se
    // gradi, pa se OpenApiSecuritySchemeReference ne moze ispravno povezati (poznat, nerazrijesen bug
    // u Swashbuckle 10 + OpenAPI.NET v2 - https://github.com/domaindrivendev/Swashbuckle.AspNetCore/issues/3731).
    // Zato je requirement globalan (lokot i na anonimnim endpointima poput /api/auth/login), ali
    // referenca je ispravna jer lambda dobija stvarni document.
    options.AddSecurityRequirement(document => new Microsoft.OpenApi.OpenApiSecurityRequirement
    {
        [new Microsoft.OpenApi.OpenApiSecuritySchemeReference("bearer", document)] = [],
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.MigrateAsync();

    var seeder = scope.ServiceProvider.GetRequiredService<Sofra.API.Data.Seed.DataSeeder>();
    await seeder.SeedAsync();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();
app.UseCors(CorsPolicyName);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapHub<NotificationHub>("/hubs/notifications");
app.MapHub<OrderHub>("/hubs/orders");

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.Run();
