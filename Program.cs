using KalsadaWatchApp.Components;
using KalsadaWatchApp.Components.Feedback;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Citizen feedback reviews. PLACEHOLDER DATA is only used in Development;
// every other environment gets the empty state until a real source is connected.
if (builder.Environment.IsDevelopment())
    builder.Services.AddSingleton<IReviewSource, MockReviewSource>();
else
    builder.Services.AddSingleton<IReviewSource, EmptyReviewSource>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
