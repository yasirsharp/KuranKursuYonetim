namespace HafizTakip.Core.Enums;

public static class UserRole
{
    public const string SuperAdmin = "super_admin";
    public const string TenantAdmin = "tenant_admin";
    public const string HeadTeacher = "head_teacher";
    public const string AssistantTeacher = "assistant_teacher";
    public const string Parent = "parent";
    public const string Student = "student";
}

public static class TenantStatus
{
    public const string Trial = "trial";
    public const string Active = "active";
    public const string Suspended = "suspended";
    public const string Cancelled = "cancelled";
}

public static class StudentStatus
{
    public const string Active = "active";
    public const string Archived = "archived";
    public const string Graduated = "graduated";
}
