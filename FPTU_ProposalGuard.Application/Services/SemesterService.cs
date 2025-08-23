using FPTU_ProposalGuard.Application.Dtos.Reviews;
using FPTU_ProposalGuard.Application.Dtos.Semesters;
using FPTU_ProposalGuard.Domain.Entities;
using FPTU_ProposalGuard.Domain.Interfaces;
using FPTU_ProposalGuard.Domain.Interfaces.Repositories;
using FPTU_ProposalGuard.Domain.Interfaces.Services;
using FPTU_ProposalGuard.Domain.Interfaces.Services.Base;
using MapsterMapper;
using Serilog;

namespace FPTU_ProposalGuard.Application.Services;

public class SemesterService(ISystemMessageService msgService,
    IUnitOfWork unitOfWork,
    IMapper mapper,
    IGenericRepository<ReviewSession, int> reviewSessionRepository,
    ILogger logger)
    : GenericService<Semester, SemesterDto, int>(msgService, unitOfWork, mapper, logger),
        ISemesterService<SemesterDto>
{
    
}