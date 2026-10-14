using HafizTakip.Core.DataAccess.EntityFramework;
using HafizTakip.DataAccess.Abstract;
using HafizTakip.Entities.Concrete;
using HafizTakip.Entities.Dtos;
using Microsoft.EntityFrameworkCore;

namespace HafizTakip.DataAccess.Concrete.EntityFramework;

public class EfStudentParentDal : EfEntityRepositoryBase<StudentParent, HafizTakipDbContext>, IStudentParentDal
{
    public EfStudentParentDal(HafizTakipDbContext context) : base(context)
    {
    }

    public Task<List<StudentParentDto>> GetRelationDetailsAsync(Guid? studentId, Guid? parentId)
    {
        var relations = Context.StudentParents.AsNoTracking();
        if (studentId.HasValue) relations = relations.Where(r => r.StudentId == studentId);
        if (parentId.HasValue) relations = relations.Where(r => r.ParentId == parentId);

        var query =
            from r in relations
            join s in Context.Students on r.StudentId equals s.Id into students
            from s in students.DefaultIfEmpty()
            join p in Context.Users on r.ParentId equals p.Id into parents
            from p in parents.DefaultIfEmpty()
            orderby r.CreatedAt
            select new StudentParentDto
            {
                Id = r.Id,
                StudentId = r.StudentId,
                StudentName = s != null ? s.FirstName + " " + s.LastName : string.Empty,
                ParentId = r.ParentId,
                ParentName = p != null ? p.FirstName + " " + p.LastName : string.Empty
            };

        return query.ToListAsync();
    }
}
