using HafizTakip.Core.Entities;

namespace HafizTakip.Entities.Dtos;

public class AnnouncementDto : IDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid AuthorId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string TargetAudience { get; set; } = string.Empty;
    public bool IsPinned { get; set; }
    public DateTime? PublishedAt { get; set; }
}

public class CreateAnnouncementDto : IDto
{
    public Guid AuthorId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string TargetAudience { get; set; } = "all";
    public bool IsPinned { get; set; } = false;
}

public class UpdateAnnouncementDto : IDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string TargetAudience { get; set; } = string.Empty;
    public bool IsPinned { get; set; }
}
