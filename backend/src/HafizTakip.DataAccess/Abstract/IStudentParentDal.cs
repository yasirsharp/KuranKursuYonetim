using HafizTakip.Core.DataAccess;
using HafizTakip.Entities.Concrete;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.DataAccess.Abstract;

public interface IStudentParentDal : IEntityRepository<StudentParent>
{
    /// <summary>Öğrenci ve veli adlarıyla eşleşmeler; studentId veya parentId ile filtrelenir.</summary>
    Task<List<StudentParentDto>> GetRelationDetailsAsync(Guid? studentId, Guid? parentId);
}
