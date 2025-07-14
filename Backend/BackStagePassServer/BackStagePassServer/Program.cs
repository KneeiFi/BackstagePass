using BackStagePassServer;
using BackStagePassServer.Services;
using BackStagePassServer.Web_sockets_stuff;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<IEmailService, EmailService>();

builder.Services.AddHostedService<BackgroundCleanupService>();

//builder.Services.AddScoped<IVideoService, VideoService>();
builder.Services.AddScoped<IVideoService, VideoServiceHLS>();

builder.Services.AddScoped<IPosterService, PosterService>();
builder.Services.AddScoped<IMovieService, MovieService>();


// Add SignalR services for real-time communication
builder.Services.AddSignalR();


builder.Services.AddDbContext<AppDbContext>(options =>
	options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
	c.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });

	c.AddSecurityDefinition("accessToken", new OpenApiSecurityScheme
	{
		Description = "Вставьте access token (без Bearer)",
		Name = "Authorization", // имя заголовка
		In = ParameterLocation.Header,
		Type = SecuritySchemeType.ApiKey,
		Scheme = "accessToken"
	});

	c.AddSecurityRequirement(new OpenApiSecurityRequirement
	{
		{
			new OpenApiSecurityScheme
			{
				Reference = new OpenApiReference
				{
					Type = ReferenceType.SecurityScheme,
					Id = "accessToken"
				}
			},
			Array.Empty<string>()
		}
	});
});

builder.Services.AddCors(options =>
{
	// Для REST API
	options.AddPolicy("Default", policy =>
	{
		policy
			.AllowAnyOrigin()
			.AllowAnyMethod()
			.AllowAnyHeader();
	});

	// Для SignalR
	options.AddPolicy("SignalR", policy =>
	{
		policy
			.WithOrigins("http://localhost:5500", "http://127.0.0.1:5500") // заменить на frontend origin (не могу вынести 2 кода в appsettings так как в финале будет 1(или больше), все равно надо будет тут менять, а после этого уже можно будет вынести)
			.AllowAnyHeader()
			.AllowAnyMethod()
			.AllowCredentials(); // обязательно для SignalR
	});
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Применяем Default для обычных запросов
app.UseCors("Default");


app.UseStaticFiles(new StaticFileOptions
{
	ContentTypeProvider = new FileExtensionContentTypeProvider
	{
		Mappings =
		{
			[".m3u8"] = "application/vnd.apple.mpegurl",
			[".ts"] = "video/mp2t"
		}
	}
});



// Применяем отдельную CORS-политику только для SignalR
app.MapHub<WatchTogetherHub>("/watch").RequireCors("SignalR");



app.UseStaticFiles();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();


app.Run();