using System.ComponentModel.DataAnnotations;

namespace Project.BLL.DTOs.Conversations;

public class TranscribeMessageRequest
{
    [StringLength(5000, ErrorMessage = "Voice text cannot exceed 5000 characters")]
    public string? VoiceText { get; set; }
}
