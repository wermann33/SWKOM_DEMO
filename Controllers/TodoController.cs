using Microsoft.AspNetCore.Mvc;
using WebApplicationTest1;

namespace ASP_Api_Demo.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TodoController : ControllerBase
    {

        private static List<TodoItem> _todoItems = new List<TodoItem>
        {
            new TodoItem { Id = 1, Name = "Task1", IsComplete = false },
            new TodoItem { Id = 2, Name = "Task2", IsComplete = true }
        };

        [HttpGet]
        public IEnumerable<TodoItem> Get()
        {
            return _todoItems;
        }

        [HttpPost]
        public ActionResult<TodoItem> PostTodoItem(TodoItem item)
        {
            item.Id = _todoItems.Max(t => t.Id) + 1; // Neue ID generieren
            _todoItems.Add(item); // Item zur Liste hinzufügen
            return CreatedAtAction(nameof(Get), new { id = item.Id }, item);
        }

        [HttpPut("{id}")]
        public IActionResult PutTodoItem(int id, TodoItem item)
        {
            var existingItem = _todoItems.FirstOrDefault(t => t.Id == id);
            if (existingItem == null)
            {
                return NotFound();
            }

            existingItem.Name = item.Name;
            existingItem.IsComplete = item.IsComplete;
            return NoContent();
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteTodoItem(int id)
        {
            var item = _todoItems.FirstOrDefault(t => t.Id == id);
            if (item == null)
            {
                return NotFound();
            }

            _todoItems.Remove(item);
            return NoContent();
        }




    }
}
