using ASP_Rest_API.DTO;
using ASP_Rest_API.Mappings;
using ASP_Rest_API.Services;
using FluentValidation;
using FluentValidation.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

//Mapping
builder.Services.AddAutoMapper(typeof(MappingProfile));

//FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<TodoItemDtoValidator>();

// CORS konfigurieren, um Anfragen von localhost:80 (WebUI) zuzulassen
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowWebUI",
        policy =>
        {
            policy.WithOrigins("http://localhost") // Die URL deiner Web-UI
                .AllowAnyHeader()
                .AllowAnyOrigin()
                .AllowAnyMethod();
        });
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

    c.IncludeXmlComments(xmlPath);
});
// Registriere HttpClient f�r den TodoController
builder.Services.AddHttpClient("TodoDAL", client =>
{
    client.BaseAddress = new Uri("http://tododal:8081"); // URL des DAL Services in Docker
});

// F�ge den RabbitMQ Background Service hinzu
builder.Services.AddControllers();
builder.Services.AddSingleton<IMessageQueueService, MessageQueueService>();
builder.Services.AddHostedService<RabbitMqListenerService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    //swagger unter http://localhost:8080/swagger/index.html fixieren (sichert gegen Konflikte durch nginx oder browser-cache oder Konfigurationsprobleme)
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "API V1");
    c.RoutePrefix = "swagger"; 
});

// Verwende die CORS-Policy
app.UseCors("AllowWebUI");

//app.UseHttpsRedirection();

// Explicitly listen to HTTP only
app.Urls.Add("http://*:8080"); // Stelle sicher, dass die App nur HTTP verwendet
app.UseAuthorization();

app.MapControllers();

app.Run();
