using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StoreItApi.Models
{
    [Table("Items")]
    public class Item
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        public string RfidTag { get; set; } = string.Empty;

        public string SerialNo { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = "AVAILABLE"; // AVAILABLE, BORROWED
        public string CurrentLocation { get; set; } = "WAREHOUSE";
        public DateTime LastSeen { get; set; } = DateTime.Now;
    }
}