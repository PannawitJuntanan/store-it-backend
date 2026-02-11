// [Table("Transactions")] // ใช้ Attribute หรือกำหนดใน OnModelCreating ก็ได้ (เลือกอย่างใดอย่างหนึ่ง)
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StoreItApi.Models
{
    [Table("Transactions")]
    public class ItemTransaction
    {
        [Key]
        public Guid Id { get; set; }

        public Guid ItemId { get; set; } // FK

        public Guid RequesterId { get; set; } // FK

        public string Type { get; set; } = string.Empty; // IN_PO, OUT_BORROW

        public string Purpose { get; set; } = string.Empty;

        public string Status { get; set; } = "PENDING";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public Guid? ApprovedBy { get; set; } // Nullable เพราะบางทีอาจยังไม่มีคนอนุมัติ

        // Navigation Properties (ตัวช่วยเวลา Join Table)
        [ForeignKey("ItemId")]
        public Item? Item { get; set; }

        [ForeignKey("RequesterId")]
        public User? Requester { get; set; }
    }
}