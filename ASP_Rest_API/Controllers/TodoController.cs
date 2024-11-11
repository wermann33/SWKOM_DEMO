using Microsoft.AspNetCore.Mvc;
using System.Text;
using ASP_Rest_API.DTO;
using ASP_Rest_API.Services;
using AutoMapper;
using RabbitMQ.Client;
using TodoDAL.Entities;
using RabbitMQ.Client.Events;
using FluentValidation;

namespace ASP_Rest_API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TodoController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMapper _mapper;
        private readonly IMessageQueueService _messageQueueService;

        public TodoController(IHttpClientFactory httpClientFactory, IMapper mapper, IMessageQueueService messageQueueService)
        {
            _httpClientFactory = httpClientFactory;
            _mapper = mapper;
            _messageQueueService = messageQueueService;
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
            Console.WriteLine($@"[PUT] Eingehender OcrText: {itemDto.OcrText}");

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
            Console.WriteLine($@"[PUT] Gemappter OcrText: {item.OcrText}");

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
                ModelState.AddModelError("taskFile", "Keine Datei hochgeladen.");
                return BadRequest(ModelState);
            }
            if (!taskFile.FileName.EndsWith(".pdf"))
            {
                ModelState.AddModelError("taskFile", "Nur PDF-Dateien sind erlaubt.");
                return BadRequest(ModelState);
            }

            // Hole den Task vom DAL
            var client = _httpClientFactory.CreateClient("TodoDAL");
            var response = await client.GetAsync($"/api/todo/{id}");
            if (!response.IsSuccessStatusCode)
            {
                return NotFound($"Fehler beim Abrufen des Tasks mit ID {id}");
            }

            // Mappe das empfangene TodoItem auf ein TodoItemDto
            var todoItem = await response.Content.ReadFromJsonAsync<TodoItem>();
            if (todoItem == null)
            {
                return NotFound($"Task mit ID {id} nicht gefunden.");
            }

            var todoItemDto = _mapper.Map<TodoItemDto>(todoItem); // Mappe TodoItem auf TodoItemDto
            todoItemDto.FileName = taskFile.FileName;

            // Validierung mit FluentValidation
            var validator = new TodoItemDtoValidator();
            var validationResult = validator.Validate(todoItemDto); // Validiere das DTO

            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.Errors);
            }

            // Mappe wieder zurück zu TodoItem, um es im DAL zu aktualisieren
            var updatedTodoItem = _mapper.Map<TodoItem>(todoItemDto);

            // Aktualisiere das Item im DAL
            var updateResponse = await client.PutAsJsonAsync($"/api/todo/{id}", updatedTodoItem);
            if (!updateResponse.IsSuccessStatusCode)
            {
                return StatusCode((int)updateResponse.StatusCode, $"Fehler beim Speichern des Dateinamens für Task {id}");
            }

            // Datei speichern (lokal im Container)
            var filePath = Path.Combine("/app/uploads", taskFile.FileName);
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!); // Erstelle das Verzeichnis, falls es nicht existiert
            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await taskFile.CopyToAsync(stream);
            }

            // Nachricht an RabbitMQ
            try
            {
                _messageQueueService.SendToQueue($"{id}|{filePath}");
                Console.WriteLine($@"File Path {filePath} an RabbitMQ Queue gesendet.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Fehler beim Senden der Nachricht an RabbitMQ: {ex.Message}");
            }

            return Ok(new { message = $"Dateiname {taskFile.FileName} für Task {id} erfolgreich gespeichert." });
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
