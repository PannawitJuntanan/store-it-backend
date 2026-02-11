using System.ComponentModel.DataAnnotations;

namespace StoreItApi.DTOs
{
    public class CreateItemDto
    {
        [Required]
        public string RfidTag { get; set; } = string.Empty;

        [Required]
        public string SerialNo { get; set; } = string.Empty;

        [Required]
        public string Name { get; set; } = string.Empty;

        public string Location { get; set; } = "WAREHOUSE";
    }

    public class UpdateItemStatusDto
    {
        public string Status { get; set; } = string.Empty; // AVAILABLE, REPAIRING
    }
}