using FPTU_ProposalGuard.Application.Common;
using FPTU_ProposalGuard.Domain.Entities;
using FPTU_ProposalGuard.Domain.Interfaces.Repositories;
using FPTU_ProposalGuard.Domain.Interfaces.Services;
using FPTU_ProposalGuard.Domain.Interfaces.Services.Base;

namespace FPTU_ProposalGuard.Application.Services;

public class QuestionService(
    IGenericRepository<ReviewQuestion, int> reviewQuestionRepository,
    ISystemMessageService msgService) : IQuestionService
{
    public async Task<IServiceResult> GetAll()
    {
        var results = await reviewQuestionRepository.GetAllAsync();
        return new ServiceResult(ResultCodeConst.SYS_Success0002,
            await msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), results);
    }
}