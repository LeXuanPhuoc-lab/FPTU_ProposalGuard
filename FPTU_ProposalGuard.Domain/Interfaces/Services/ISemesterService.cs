using FPTU_ProposalGuard.Domain.Entities;

namespace FPTU_ProposalGuard.Domain.Interfaces.Services.Base;

public interface ISemesterService<TDto> : IGenericService<Semester, TDto, int>
    where TDto : class
{
}