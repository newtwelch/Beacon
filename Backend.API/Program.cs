using Backend.API.Wrapper;
using Backend.Core.Services;
using SQLite;

var builder = WebApplication.CreateBuilder(args);


string appData = AppContext.BaseDirectory;
string songDbPath = Path.Combine(appData, "Database", "songs.db");
Directory.CreateDirectory(Path.GetDirectoryName(songDbPath)!);

builder.Services.AddSingleton(new SongDbConnection(songDbPath));

builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
