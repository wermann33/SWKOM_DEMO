using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using ASP_Rest_API.DTO;

namespace ASP_Rest_API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TodoController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public TodoController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var client = _httpClientFactory.CreateClient("TodoDAL");
            var response = await client.GetAsync("/api/todo"); // Endpunkt des DAL

            if (response.IsSuccessStatusCode)
            {
                var items = await response.Content.ReadFromJsonAsync<IEnumerable<TodoItemDto>>();
                return Ok(items);
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
                var item = await response.Content.ReadFromJsonAsync<TodoItemDto>();
                if (item != null)
                {
                    return Ok(item);
                }
                return NotFound();
            }

            return StatusCode((int)response.StatusCode, "Error retrieving Todo item from DAL");
        }

        [HttpPost]
        public async Task<IActionResult> Create(TodoItemDto item)
        {
            var client = _httpClientFactory.CreateClient("TodoDAL");
            var response = await client.PostAsJsonAsync("/api/todo", item);

            if (response.IsSuccessStatusCode)
            {
                return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
            }

            return StatusCode((int)response.StatusCode, "Error creating Todo item in DAL");
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, TodoItemDto item)
        {
            if (id != item.Id)
            {
                return BadRequest("ID mismatch");
            }

            var client = _httpClientFactory.CreateClient("TodoDAL");
            var response = await client.PutAsJsonAsync($"/api/todo/{id}", item);

            if (response.IsSuccessStatusCode)
            {
                return NoContent();
            }

            return StatusCode((int)response.StatusCode, "Error updating Todo item in DAL");
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
