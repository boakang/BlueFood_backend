using BlueFood.Api.Serialization;
using BlueFood.Api.Services;

LoadEnvironmentFile();

var builder = WebApplication.CreateBuilder(args);

const string FrontendCorsPolicy = "FrontendCorsPolicy";

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new VietnamDateTimeJsonConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IBatchService, BatchService>();
builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
    {
        policy
            .WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors(FrontendCorsPolicy);
app.MapControllers();
app.Run();

static void LoadEnvironmentFile()
{
    var currentDirectory = new DirectoryInfo(Directory.GetCurrentDirectory());

    while (currentDirectory is not null)
    {
        var envFilePath = Path.Combine(currentDirectory.FullName, ".env");
        if (File.Exists(envFilePath))
        {
            foreach (var line in File.ReadAllLines(envFilePath))
            {
                var trimmedLine = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith('#') || trimmedLine.StartsWith("//"))
                {
                    continue;
                }

                var equalsIndex = trimmedLine.IndexOf('=');
                if (equalsIndex <= 0)
                {
                    continue;
                }

                var key = trimmedLine[..equalsIndex].Trim();
                var value = trimmedLine[(equalsIndex + 1)..].Trim().Trim('"');

                if (string.IsNullOrWhiteSpace(key) || Environment.GetEnvironmentVariable(key) is not null)
                {
                    continue;
                }

                Environment.SetEnvironmentVariable(key, value);
            }

            break;
        }

        currentDirectory = currentDirectory.Parent;
    }
}
