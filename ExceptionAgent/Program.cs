using ExceptionAgent.Application.Email;
using ExceptionAgent.Application.Exceptions;
using ExceptionAgent.Application.Allocation;
using ExceptionAgent.Application.Allocation.Policies;
using ExceptionAgent.Data;
using ExceptionAgent.Infraestructure.AI;
using Microsoft.EntityFrameworkCore;

namespace ExceptionAgent
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite(
                    builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddScoped<ExceptionDetector>();
            builder.Services.AddScoped<ExceptionInvestigationService>();
            builder.Services.AddHttpClient<AgentService>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(60);
            });
            builder.Services.AddHttpClient<EmailExtractionService>();
            builder.Services.AddScoped<EmailIngestionService>();
            builder.Services.AddScoped<EmailMatchingService>();
            builder.Services.AddScoped<ExceptionRiskCalculationService>();
            builder.Services.AddScoped<AllocationEngine>();
            builder.Services.AddScoped<IAllocationPolicy, DatePriorityAllocationPolicy>();
            builder.Services.AddScoped<AllocationScenarioBuilder>();
            builder.Services.AddScoped<AllocationDataService>();
            builder.Services.AddScoped<AllocationScenarioService>();
            builder.Services.AddScoped<AllocationImpactService>();
            builder.Services.AddScoped<ScenarioImpactCalculator>();

            // Add services to the container.
            builder.Services.AddRazorPages();

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                DbSeeder.Seed(context);

                var emailIngestionService =
                    scope.ServiceProvider.GetRequiredService<EmailIngestionService>();

                await emailIngestionService.ProcessEmailsAsync();
            }

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.MapStaticAssets();
            app.MapRazorPages()
               .WithStaticAssets();

            app.Run();
        }
    }
}
