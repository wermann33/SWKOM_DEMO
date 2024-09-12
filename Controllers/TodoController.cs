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


        /// <summary>
        /// Gibt eine Liste von TodoItems zurück. Optional können Name und Erledigungsstatus gefiltert werden.
        /// </summary>
        /// <param name="name">Der Name des TodoItems, nach dem gefiltert werden soll(optional)</param>
        /// <param name="isComplete">Der Status, ob das TodoItem abgeschlossen ist oder nicht (optional)</param>
        /// <returns>Eine optional gefilterte Liste von TodoItems</returns>
   
        [HttpGet]
        public IEnumerable<TodoItem> Get([FromQuery] string? name, [FromQuery] bool? isComplete)
        {
            var items = _todoItems.AsEnumerable();

            //Nach Name filtern, wenn ein Name übergeben wurde
            if (!string.IsNullOrWhiteSpace(name))
            {
                items = items.Where(t => t.Name.Contains(name));
            }

            //Nach Erledigungsstatus filter, wenn einer übergeben wurde
            if (isComplete.HasValue)
            {
                items = items.Where(t => t.IsComplete == isComplete.Value);
            }

            return items;
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
