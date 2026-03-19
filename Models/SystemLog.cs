using System.ComponentModel.DataAnnotations;

namespace SmartHomeDashboard.Models
{
    public class SystemLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Type { get; set; } = ""; // device, system, alert, automation

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = "";

        [StringLength(500)]
        public string Description { get; set; } = "";

        [StringLength(100)]
        public string DeviceName { get; set; } = "";

        [StringLength(50)]
        public string Icon { get; set; } = "";

        public DateTime Timestamp { get; set; } = DateTime.Now;

        [StringLength(50)]
        public string TimeString { get; set; } = "";

        // 索引
        public bool IsRead { get; set; } = false;
    }
}