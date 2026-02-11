using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StoreItApi.Data;
using StoreItApi.DTOs;
using StoreItApi.Models;

namespace StoreItApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // บังคับ Login ทุกคำสั่ง
    public class ItemsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ItemsController(AppDbContext context)
        {
            _context = context;
        }

        // 1. ดูของทั้งหมดในคลัง (GET /api/items)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Item>>> GetItems()
        {
            return await _context.Items.ToListAsync();
        }

        // 2. ดูรายละเอียดรายชิ้น จาก RFID (GET /api/items/rfid/{tag})
        // * อันนี้สำคัญ! เครื่องสแกนจะยิงมาเช็คสถานะจากเส้นนี้
        [HttpGet("rfid/{tag}")]
        public async Task<ActionResult<Item>> GetItemByRfid(string tag)
        {
            var item = await _context.Items.FirstOrDefaultAsync(i => i.RfidTag == tag);

            if (item == null)
            {
                return NotFound(new { message = "ไม่พบอุปกรณ์นี้ในระบบ" });
            }

            return item;
        }

        // 3. ลงทะเบียนของใหม่ (POST /api/items)
        [HttpPost]
        public async Task<ActionResult<Item>> CreateItem(CreateItemDto request)
        {
            // เช็คว่า RFID หรือ Serial ซ้ำไหม
            if (await _context.Items.AnyAsync(i => i.RfidTag == request.RfidTag))
            {
                return BadRequest("RFID Tag นี้มีในระบบแล้ว");
            }

            var newItem = new Item
            {
                Name = request.Name,
                RfidTag = request.RfidTag,
                SerialNo = request.SerialNo,
                CurrentLocation = request.Location,
                Status = "AVAILABLE",
                LastSeen = DateTime.Now
            };

            _context.Items.Add(newItem);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetItemByRfid), new { tag = newItem.RfidTag }, newItem);
        }

        // 4. ลบอุปกรณ์ (DELETE /api/items/{id})
        [HttpDelete("{id}")]
        [Authorize(Roles = "ADMIN")] // เฉพาะ Admin เท่านั้น
        public async Task<IActionResult> DeleteItem(Guid id)
        {
            var item = await _context.Items.FindAsync(id);
            if (item == null) return NotFound();

            _context.Items.Remove(item);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}