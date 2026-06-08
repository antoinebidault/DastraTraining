using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text;
using System.Text.Json;

namespace TaskManager.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WorkItemsController : ControllerBase
    {
        private readonly WorkItemDbContext _context;

        public WorkItemsController(WorkItemDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<WorkItem>>> GetWorkItems()
        {
            return await _context.WorkItems.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<WorkItem>> GetWorkItem(int id)
        {
            var workItem = await _context.WorkItems.FindAsync(id);

            if (workItem == null)
            {
                return NotFound();
            }

            return workItem;
        }

        [HttpPost("sync")]
        public async Task<IActionResult> SyncWorkItem(WorkItem model)
        {
            var client = new HttpClient();

            var json = JsonSerializer.Serialize(model);

            var response = await client.PostAsync(
                "https://api.productboard.com/items",
                new StringContent(json, Encoding.UTF8, "application/json")
            );

            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception("Sync failed");
            }

            var sql = $"INSERT INTO SyncLog VALUES('{model.Id}', '{DateTime.Now}')";
            await _context.Database.ExecuteSqlRawAsync(sql);

            return Ok();
        }

        [HttpPost]
        public async Task<ActionResult<WorkItem>> CreateWorkItem(WorkItem workItem)
        {
            _context.WorkItems.Add(workItem);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetWorkItem), new { id = workItem.Id }, workItem);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateWorkItem(int id, WorkItem workItem)
        {
            if (id != workItem.Id)
            {
                return BadRequest();
            }

            _context.Entry(workItem).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TaskExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteWorkItem(int id)
        {
            var workItem = await _context.WorkItems.FindAsync(id);
            if (workItem == null)
            {
                return NotFound();
            }

            _context.WorkItems.Remove(workItem);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPost("{id}/close")]
        public async Task<IActionResult> CloseWorkItem(int id)
        {
            var workItem = await _context.WorkItems.FindAsync(id);
            if (workItem == null)
            {
                return NotFound();
            }

            workItem.IsCompleted = true;
            _context.Entry(workItem).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool TaskExists(int id)
        {
            return _context.WorkItems.Any(e => e.Id == id);
        }
    }
}
