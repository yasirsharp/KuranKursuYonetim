using System;
using System.Collections.Generic;

namespace HafizTakip.Entities.Dtos;

// --- ATTENDANCE DTOs ---

public class AttendanceDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid StudentId { get; set; }
    public string StudentName { get; set; }
    public DateTime Date { get; set; }
    public string Status { get; set; } // present, absent, late, excused
    public string Notes { get; set; }
}

public class CreateAttendanceDto
{
    public Guid StudentId { get; set; }
    public DateTime Date { get; set; }
    public string Status { get; set; } // present, absent, late, excused
    public string Notes { get; set; }
}

public class BulkAttendanceDto
{
    public DateTime Date { get; set; }
    public List<CreateAttendanceItemDto> Attendances { get; set; }
}

public class CreateAttendanceItemDto
{
    public Guid StudentId { get; set; }
    public string Status { get; set; }
    public string Notes { get; set; }
}

// --- REPETITION SESSION DTOs ---

public class RepetitionSessionDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid StudentId { get; set; }
    public Guid TeacherId { get; set; }
    public DateTime SessionDate { get; set; }
    public int? StartCuz { get; set; }
    public int? EndCuz { get; set; }
    public int? StartPage { get; set; }
    public int? EndPage { get; set; }
    public string Performance { get; set; }
    public string TeacherNotes { get; set; }
}

public class CreateRepetitionSessionDto
{
    public Guid StudentId { get; set; }
    public Guid TeacherId { get; set; }
    public DateTime SessionDate { get; set; }
    public int? StartCuz { get; set; }
    public int? EndCuz { get; set; }
    public int? StartPage { get; set; }
    public int? EndPage { get; set; }
    public string Performance { get; set; }
    public string TeacherNotes { get; set; }
}

/// <summary>Yoklama listesi filtreleri (query string'den bağlanır).</summary>
public class AttendanceFilter
{
    public Guid? StudentId { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
}