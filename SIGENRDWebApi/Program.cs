
using SIGENRD.Core.Application;
using SIGENRD.Infrastructure.Identity;
using SIGENRD.Infrastructure.Persistences;
using SIGENRD.Presentation.WebApi;

var builder = WebApplication.CreateBuilder(args);

// ===========================================================
// 🧱 REGISTRO DE CAPAS
// ===========================================================
builder.Services.AddApplicationLayer();
builder.Services.AddInfrastructurePersistence(builder.Configuration);
builder.Services.AddIdentityInfrastructure(builder.Configuration);
builder.Services.AddPresentationLayer();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHealthChecks();

// 🔹 CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

// 🔹 Controllers & Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddOpenApi();

// ===========================================================
// 🧱 PIPELINE
// ===========================================================
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();
app.UseHealthChecks("/health");

app.MapControllers();
app.Run();

