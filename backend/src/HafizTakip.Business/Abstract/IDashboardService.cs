using HafizTakip.Core.Utilities.Results;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.Business.Abstract;

public interface IDashboardService
{
    Task<IDataResult<DashboardSummaryDto>> GetSummaryAsync();
    Task<IDataResult<DashboardStudentProgressDto>> GetStudentProgressAsync(Guid studentId);
    Task<IDataResult<DashboardGroupOverviewDto>> GetGroupOverviewAsync(Guid groupId);
}
