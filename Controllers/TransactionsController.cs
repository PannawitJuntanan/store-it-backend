using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StoreItApi.Data;
using StoreItApi.DTOs;
using StoreItApi.Models;
using System.Security.Claims;

namespace StoreItApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TransactionsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TransactionsController(AppDbContext context)
        {
            _context = context;
        }
        // GET: api/Transactions
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ItemTransaction>>> GetTransactions()
        {
            // ใช้ .Include เพื่อดึงข้อมูลจากตาราง Item และ User มาแสดงชื่อด้วย
            return await _context.Transactions
                .Include(t => t.Item)
                .Include(t => t.Requester)
                .OrderByDescending(t => t.CreatedAt) // เอาล่าสุดขึ้นก่อน
                .ToListAsync();
        }
        // ==========================================
        //  USER FLOW: ขอเบิกของ
        // ==========================================

        [HttpPost("request")]
        public async Task<IActionResult> CreateRequest(CreateTransactionDto request)
        {
            // 1. หา User ID จาก Token คนที่ Login
            var userId = User.FindFirst("id")?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            // 2. เช็คว่าของชิ้นนี้มีอยู่จริงไหม
            var item = await _context.Items.FirstOrDefaultAsync(i => i.RfidTag == request.RfidTag);
            if (item == null) return NotFound("ไม่พบอุปกรณ์นี้ในระบบ");

            // 3. เช็คสถานะ: ของต้อง AVAILABLE ถึงจะยืมได้
            if (item.Status != "AVAILABLE")
            {
                return BadRequest($"อุปกรณ์นี้สถานะไม่พร้อมใช้งาน (Status: {item.Status})");
            }

            // 4. สร้างใบคำขอ (Status = PENDING)
            var transaction = new ItemTransaction
            {
                ItemId = item.Id,
                RequesterId = Guid.Parse(userId),
                Type = request.Type,
                Purpose = request.Purpose,
                Status = "PENDING", // รออนุมัติ
                CreatedAt = DateTime.Now
            };

            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            return Ok(new { message = "สร้างคำขอสำเร็จ รอ IT อนุมัติ", transactionId = transaction.Id });
        }

        // ==========================================
        //  IT FLOW: อนุมัติ / ปฏิเสธ
        // ==========================================

        [HttpPost("approve/{id}")]
        // [Authorize(Roles = "ADMIN,IT")] // เปิดใช้บรรทัดนี้ถ้าอยากจำกัดสิทธิ์
        public async Task<IActionResult> ApproveRequest(Guid id, ApproveTransactionDto input)
        {
            var userId = User.FindFirst("id")?.Value;

            var transaction = await _context.Transactions.FindAsync(id);
            if (transaction == null) return NotFound("ไม่พบรายการนี้");

            if (transaction.Status != "PENDING") return BadRequest("รายการนี้ถูกดำเนินการไปแล้ว");

            // อัปเดตสถานะ
            transaction.Status = input.IsApproved ? "APPROVED" : "REJECTED";
            transaction.ApprovedBy = Guid.Parse(userId!);

            await _context.SaveChangesAsync();

            return Ok(new { message = $"รายการถูก {(input.IsApproved ? "อนุมัติ" : "ปฏิเสธ")} แล้ว" });
        }

        // ==========================================
        //  SYSTEM FLOW: RFID SCAN (หัวใจสำคัญ!)
        //  API นี้จะถูกเรียกเมื่อของผ่านประตู (Gate)
        // ==========================================

        [HttpPost("scan")]
        [AllowAnonymous] // อนุญาตให้เครื่อง Scan ยิงเข้ามาได้โดยไม่ต้อง Login (หรือจะใช้ API Key ก็ได้)
        public async Task<IActionResult> HandleRfidScan(RfidScanDto input)
        {
            var item = await _context.Items.FirstOrDefaultAsync(i => i.RfidTag == input.RfidTag);
            if (item == null) return NotFound("Unknown Tag");

            // CASE 1: ของกำลังจะออก (Check-Out)
            // เงื่อนไข: สถานะของยังเป็น AVAILABLE แต่มีใบอนุมัติ (APPROVED) ค้างอยู่
            if (item.Status == "AVAILABLE")
            {
                var approvedTx = await _context.Transactions
                    .OrderByDescending(t => t.CreatedAt)
                    .FirstOrDefaultAsync(t => t.ItemId == item.Id && t.Status == "APPROVED");

                if (approvedTx != null)
                {
                    // เปลี่ยนสถานะของ -> BORROWED
                    item.Status = "BORROWED";
                    item.CurrentLocation = "OUT_WITH_USER"; // หรือชื่อ User ที่ยืม
                    item.LastSeen = DateTime.Now;

                    // ปิดใบงาน -> COMPLETED
                    approvedTx.Status = "COMPLETED";

                    await _context.SaveChangesAsync();
                    return Ok(new { action = "CHECK_OUT", item = item.Name, message = "อนุญาตให้นำออกได้" });
                }
                else
                {
                    // เจอของ AVAILABLE เดินออก แต่ไม่มีใบอนุมัติ! -> เตือน!
                    return BadRequest(new { action = "ALERT", message = "ของชิ้นนี้ยังไม่ได้รับอนุมัติให้นำออก!" });
                }
            }

            // CASE 2: ของกลับมาคืน (Check-In)
            // เงื่อนไข: ของสถานะ BORROWED แล้วกลับมาโผล่ที่ Gate
            else if (item.Status == "BORROWED")
            {
                // รับของคืนอัตโนมัติ
                item.Status = "AVAILABLE";
                item.CurrentLocation = "WAREHOUSE";
                item.LastSeen = DateTime.Now;

                // สร้าง Transaction ขาเข้า (Auto Return)
                var returnTx = new ItemTransaction
                {
                    ItemId = item.Id,
                    RequesterId = await GetLastBorrowerId(item.Id), // ฟังก์ชันหาคนยืมล่าสุด (Optional)
                    Type = "IN_RETURN",
                    Status = "COMPLETED",
                    Purpose = "Auto Return by RFID",
                    CreatedAt = DateTime.Now
                };

                _context.Transactions.Add(returnTx);
                await _context.SaveChangesAsync();

                return Ok(new { action = "CHECK_IN", item = item.Name, message = "รับคืนสำเร็จ" });
            }

            return Ok(new { message = "Update Last Seen", tag = input.RfidTag });
        }

        // Helper Function: หาว่าใครเป็นคนยืมคนล่าสุด
        private async Task<Guid> GetLastBorrowerId(Guid itemId)
        {
            var lastTx = await _context.Transactions
                .Where(t => t.ItemId == itemId && t.Type == "OUT_BORROW")
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefaultAsync();

            return lastTx?.RequesterId ?? Guid.Empty; // ถ้าหาไม่เจอคืนค่าว่าง
        }
    }
}