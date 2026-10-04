using HafizTakip.Core.Entities;

namespace HafizTakip.Entities.Dtos;

public class GroupDto : IDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid TeacherId { get; set; }
    public string TeacherName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class CreateGroupDto : IDto
{
    public Guid TeacherId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class UpdateGroupDto : IDto
{
    public Guid Id { get; set; }
    public Guid TeacherId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
