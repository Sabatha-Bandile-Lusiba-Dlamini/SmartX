namespace SmartX.Api.Models
{
    public class Achievement
    {
        public string Code { get; set; } = string.Empty; 
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool Unlocked { get; set; }
    }
}
