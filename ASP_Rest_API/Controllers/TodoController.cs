using Microsoft.AspNetCore.Mvc;
using System.Text;
using ASP_Rest_API.DTO;
using AutoMapper;
using RabbitMQ.Client;
using TodoDAL.Entities;

namespace ASP_Rest_API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TodoController : ControllerBase, IDisposable
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMapper _mapper;
        private readonly IConnection _connection;
        private readonly IModel _channel;

        public TodoController(IHttpClientFactory httpClientFactory, IMapper mapper)
        {
            _httpClientFactory = httpClientFactory;
            _mapper = mapper;

            // Stelle die Verbindung zu RabbitMQ her
            var factory = new ConnectionFactory() { HostName = "rabbitmq", UserName = "user", Password = "password" };
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            // Deklariere die Queue
            _channel.QueueDeclare(queue: "file_queue", durable: false, exclusive: false, autoDelete: false, arguments: null);
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var client = _httpClientFactory.CreateClient("TodoDAL");
            var response = await client.GetAsync("/api/todo"); // Endpunkt des DAL

            if (response.IsSuccessStatusCode)
            {
                var items = await response.Content.ReadFromJsonAsync<IEnumerable<TodoItem>>();
                var sortedItems = items.OrderBy(item => item.Id);
                var dtoItems = _mapper.Map<IEnumerable<TodoItemDto>>(sortedItems);
                return Ok(dtoItems);
            }

            return StatusCode((int)response.StatusCode, "Error retrieving Todo items from DAL");
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var client = _httpClientFactory.CreateClient("TodoDAL");
            var response = await client.GetAsync($"/api/todo/{id}");

            if (response.IsSuccessStatusCode)
            {
                var item = await response.Content.ReadFromJsonAsync<TodoItem>();
                var dtoItem = _mapper.Map<TodoItemDto>(item);
                if (item != null)
                {
                    return Ok(dtoItem);
                }
                return NotFound();
            }

            return StatusCode((int)response.StatusCode, "Error retrieving Todo item from DAL");
        }

        [HttpPost]
        public async Task<IActionResult> Create(TodoItemDto itemDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var client = _httpClientFactory.CreateClient("TodoDAL");
            var item = _mapper.Map<TodoItem>(itemDto);
            var response = await client.PostAsJsonAsync("/api/todo", item);

            if (response.IsSuccessStatusCode)
            {
                return CreatedAtAction(nameof(GetById), new { id = item.Id }, itemDto);
            }

            return StatusCode((int)response.StatusCode, "Error creating Todo item in DAL");
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, TodoItemDto itemDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != itemDto.Id)
            {
                return BadRequest("ID mismatch");
            }

            var client = _httpClientFactory.CreateClient("TodoDAL");
            var item = _mapper.Map<TodoItem>(itemDto);
            var response = await client.PutAsJsonAsync($"/api/todo/{id}", item);

            if (response.IsSuccessStatusCode)
            {
                return NoContent();
            }

            return StatusCode((int)response.StatusCode, "Error updating Todo item in DAL");
        }

        [HttpPut("{id}/upload")]
        public async Task<IActionResult> UploadFile(int id, IFormFile? taskFile)
        {
            if (taskFile == null || taskFile.Length == 0)
            {
                return BadRequest("Keine Datei hochgeladen.");
            }

            // Hole den Task vom DAL
            var client = _httpClientFactory.CreateClient("TodoDAL");
            var response = await client.GetAsync($"/api/todo/{id}");
            if (!response.IsSuccessStatusCode)
            {
                return NotFound($"Fehler beim Abrufen des Tasks mit ID {id}");
            }

            // Mappe das empfangene TodoItem auf ein TodoItemDto
            var todoItem = await response.Content.ReadFromJsonAsync<TodoItemDto>();
            if (todoItem == null)
            {
                return NotFound($"Task mit ID {id} nicht gefunden.");
            }

            var todoItemDto = _mapper.Map<TodoItem>(todoItem);

            // Setze den Dateinamen im DTO
            todoItemDto.FileName = taskFile.FileName;

            // Aktualisiere das Item im DAL, nutze das DTO
            var updateResponse = await client.PutAsJsonAsync($"/api/todo/{id}", todoItemDto);
            if (!updateResponse.IsSuccessStatusCode)
            {
                return StatusCode((int)updateResponse.StatusCode, $"Fehler beim Speichern des Dateinamens für Task {id}");
            }

            // Nachricht an RabbitMQ
            try
            {
                SendToMessageQueue(taskFile.FileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Fehler beim Senden der Nachricht an RabbitMQ: {ex.Message}");
            }

            return Ok(new { message = $"Dateiname {taskFile.FileName} für Task {id} erfolgreich gespeichert." });
        }

        private void SendToMessageQueue(string fileName)
        {
            // Sende die Nachricht in den RabbitMQ channel/queue
            var body = Encoding.UTF8.GetBytes(fileName);
            _channel.BasicPublish(exchange: "", routingKey: "file_queue", basicProperties: null, body: body);
            Console.WriteLine($@"[x] Sent {fileName}");
        }

        public void Dispose()
        {
            _channel?.Close();
            _connection?.Close();
        }


        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var client = _httpClientFactory.CreateClient("TodoDAL");
            var response = await client.DeleteAsync($"/api/todo/{id}");

            if (response.IsSuccessStatusCode)
            {
                return NoContent();
            }

            return StatusCode((int)response.StatusCode, "Error deleting Todo item from DAL");
        }
    }
}
