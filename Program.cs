using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StoreItApi.Data;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 1. เชื่อมต่อ SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. ตั้งค่า JWT Auth
var jwtKey = builder.Configuration["JwtSettings:Key"];
var key = Encoding.ASCII.GetBytes(jwtKey!);

builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(x =>
{
    x.RequireHttpsMetadata = false; // รัน Local ไม่ต้องบังคับ HTTPS
    x.SaveToken = true;
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false
    };
});
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()   // อนุญาตทุกเว็บ (รวมถึง localhost:5173)
              .AllowAnyMethod()   // อนุญาตทุก HTTP Verb (GET, POST, etc.)
              .AllowAnyHeader();  // อนุญาตทุก Header (Authorization, Content-Type)
    });
});
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 3. Auto Migration (สร้างตารางให้อัตโนมัติเมื่อรัน)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated(); // ถ้ายังไม่มี DB ให้สร้างใหม่เลย
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseCors("AllowAll");

app.UseAuthentication(); // <--- อย่าลืมบรรทัดนี้
app.UseAuthorization();

app.MapControllers();

app.Run();