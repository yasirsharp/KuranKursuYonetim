using HafizTakip.Core.Entities;

namespace HafizTakip.Entities.Dtos;

public class StudentParentDto : IDto
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public Guid ParentId { get; set; }
    public string ParentName { get; set; } = string.Empty;
}

public class CreateStudentParentDto : IDto
{
    public Guid StudentId { get; set; }
    public Guid ParentId { get; set; }
}
