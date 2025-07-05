using EPS.SignalR;

var builder = WebApplication.CreateBuilder(args);

// Configuration & Services
builder.Services.Configure<DiscountOptions>(builder.Configuration.GetSection("Discount"));
builder.Services.AddSignalR();

// Add named CORS policy for frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5004") // Match frontend port
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials(); // Important for WebSockets
    });
});

var app = builder.Build();

// Middleware order is critical
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseRouting();                      // 🔹 Routing comes first
app.UseCors("AllowFrontend");         // 🔹 Apply named policy AFTER routing
app.MapHub<DiscountHub>("/discountHub"); // 🔹 Map SignalR endpoint after CORS

app.Run();