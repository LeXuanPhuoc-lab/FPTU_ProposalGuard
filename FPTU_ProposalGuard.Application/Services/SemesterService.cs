using FPTU_ProposalGuard.Application.Common;
using FPTU_ProposalGuard.Application.Dtos.Semesters;
using FPTU_ProposalGuard.Domain.Common.Enums;
using FPTU_ProposalGuard.Domain.Entities;
using FPTU_ProposalGuard.Domain.Interfaces;
using FPTU_ProposalGuard.Domain.Interfaces.Services;
using FPTU_ProposalGuard.Domain.Interfaces.Services.Base;
using FPTU_ProposalGuard.Domain.Specifications;
using MapsterMapper;
using Serilog;

namespace FPTU_ProposalGuard.Application.Services;

public class SemesterService(
    ISystemMessageService msgService,
    IUnitOfWork unitOfWork,
    IMapper mapper,
    ILogger logger)
    : GenericService<Semester, SemesterDto, int>(msgService, unitOfWork, mapper, logger),
        ISemesterService<SemesterDto>
{
    public async Task<IServiceResult> GetCurrentSemester()
    {
        int month = DateTime.Now.Month;
        int year = DateTime.Now.Year % 100; // lấy 2 số cuối
        string semester;

        if (month >= 1 && month <= 4)
            semester = "SP";
        else if (month >= 5 && month <= 8)
            semester = "SU";
        else
            semester = "FA";

        var semesterCode = $"{semester}{year:D2}";

        var semesterEntity = await unitOfWork.Repository<Semester, int>().GetWithSpecAsync(
            new BaseSpecification<Semester>(s => s.SemesterCode.Equals(semesterCode)));

        if (semesterEntity is null)
        {
            semesterEntity = new Semester()
            {
                SemesterCode = semesterCode,
                Year = DateTime.Now.Year,
                Term = Term.Fall,
            };
            await unitOfWork.Repository<Semester, int>().AddAsync(semesterEntity);
        }

        return new ServiceResult(ResultCodeConst.SYS_Success0002,
            await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
            semesterEntity);
    }
}