using Azure.Core;
using Azure.Identity;
using CallCenter;
using CallCenter.Options;
using Microsoft.Extensions.Azure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

builder.Services.AddOptions<SpeechToTextOptions>()
    .BindConfiguration(nameof(SpeechToTextOptions))
    .ValidateDataAnnotations();
builder.Services.AddOptions<TextAnalyticsOptions>()
    .BindConfiguration(nameof(TextAnalyticsOptions))
    .ValidateDataAnnotations();
builder.Services.AddOptions<AOAIOptions>()
    .BindConfiguration(nameof(AOAIOptions))
    .ValidateDataAnnotations();

TokenCredential tokenCredential = builder.Environment.IsDevelopment()
    ? new AzureCliCredential()
    : new DefaultAzureCredential();
builder.Services.AddSingleton(tokenCredential);

builder.Services.AddAzureClients(clientBuilder =>
{
    clientBuilder.AddTextAnalyticsClient(builder.Configuration.GetSection(nameof(TextAnalyticsOptions)));
    clientBuilder.AddOpenAIClient(builder.Configuration.GetSection(nameof(AOAIOptions)));
    clientBuilder.UseCredential(tokenCredential);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.MapCallCenterApis();

app.Run();
