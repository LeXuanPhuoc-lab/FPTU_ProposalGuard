using FPTU_ProposalGuard.Application.Dtos.Reviews;
using FPTU_ProposalGuard.Application.Services.IExternalServices;
using FPTU_ProposalGuard.Domain.Entities;
using FPTU_ProposalGuard.Domain.Interfaces;
using FPTU_ProposalGuard.Domain.Interfaces.Repositories;
using FPTU_ProposalGuard.Domain.Interfaces.Services;
using MapsterMapper;
using Serilog;

namespace FPTU_ProposalGuard.Application.Services;

public class ReviewQuestionService(
    ISystemMessageService msgService,
    IUnitOfWork unitOfWork,
    IMapper mapper,
    IGenericRepository<ReviewQuestion, int> reviewQuestionRepository,
    IS3Service s3Service,
    ILogger logger)
    : GenericService<ReviewQuestion, ReviewQuestionDto, int>(msgService, unitOfWork, mapper, logger),
        IReviewQuestionService<ReviewQuestionDto>
{
}