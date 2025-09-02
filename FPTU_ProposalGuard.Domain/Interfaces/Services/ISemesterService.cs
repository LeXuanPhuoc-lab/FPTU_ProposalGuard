using FPTU_ProposalGuard.Domain.Entities;
using FPTU_ProposalGuard.Domain.Interfaces.Services.Base;

namespace FPTU_ProposalGuard.Domain.Interfaces.Services;

public interface ISemesterService<TDto> : IGenericService<Semester, TDto, int>
    where TDto : class
{
    Task<IServiceResult> GetCurrentSemester();
    Task<IServiceResult> GetSemesters();
}