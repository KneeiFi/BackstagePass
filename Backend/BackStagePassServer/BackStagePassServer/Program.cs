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
		Description = "�������� access token (��� Bearer)",
		Name = "Authorization", // ��� ���������
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
	// ��� REST API
	options.AddPolicy("Default", policy =>
	{
		policy
			.AllowAnyOrigin()
			.AllowAnyMethod()
			.AllowAnyHeader();
	});

	// ��� SignalR
	options.AddPolicy("SignalR", policy =>
	{
		policy
			.WithOrigins("http://localhost:5500", "http://127.0.0.1:5500") // �������� �� frontend origin (�� ���� ������� 2 ���� � appsettings ��� ��� � ������ ����� 1(��� ������), ��� ����� ���� ����� ��� ������, � ����� ����� ��� ����� ����� �������)
			.AllowAnyHeader()
			.AllowAnyMethod()
			.AllowCredentials(); // ����������� ��� SignalR
	});
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ��������� Default ��� ������� ��������
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



// ��������� ��������� CORS-�������� ������ ��� SignalR
app.MapHub<WatchTogetherHub>("/watch").RequireCors("SignalR");



app.UseStaticFiles();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();


app.Run();