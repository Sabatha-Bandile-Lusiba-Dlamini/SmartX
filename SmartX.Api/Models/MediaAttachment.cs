namespace SmartX.Api.Models
{ 
public class MediaAttachment
    {
        public int Id { get; set; }
        public int SensorId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; }
    }
}