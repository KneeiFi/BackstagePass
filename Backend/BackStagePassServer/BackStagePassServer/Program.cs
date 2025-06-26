using BackStagePassServer;
using BackStagePassServer.Services;
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
	options.AddDefaultPolicy(policy =>
	{
		policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
	});
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();

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

app.UseStaticFiles();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();