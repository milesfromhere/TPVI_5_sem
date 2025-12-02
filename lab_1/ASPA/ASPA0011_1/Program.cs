var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    
}

app.UseHttpsRedirection();
app.UseRouting();

app.MapControllers();

app.Run();
