using System.ComponentModel.DataAnnotations;

namespace StoreItApi.DTOs
{
    // 1. สำหรับพนักงานส่งคำขอ (ยืม/ย้าย)
    public class CreateTransactionDto
    {
        [Required]
        public string RfidTag { get; set; } = string.Empty; // ใช้ RFID สะดวกกว่า ID

        [Required]
        public string Type { get; set; } = "OUT_BORROW"; // OUT_BORROW, OUT_TRANSFER

        public string Purpose { get; set; } = string.Empty; // ยืมไปทำไม
    }

    // 2. สำหรับ IT กดอนุมัติ
    public class ApproveTransactionDto
    {
        [Required]
        public bool IsApproved { get; set; } // true = อนุมัติ, false = ปฏิเสธ
    }

    // 3. สำหรับเครื่อง RFID Reader ยิงเข้ามา (Webhook)
    public class RfidScanDto
    {
        [Required]
        public string RfidTag { get; set; } = string.Empty;

        public string ReaderLocation { get; set; } = "GATE_1"; // ประตูไหน
    }
}