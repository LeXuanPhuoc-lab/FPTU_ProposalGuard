using System.Text.Json;
using FPTU_ProposalGuard.Application.Common;
using FPTU_ProposalGuard.Application.Dtos;
using FPTU_ProposalGuard.Application.Dtos.Notifications;
using FPTU_ProposalGuard.Application.Dtos.Proposals;
using FPTU_ProposalGuard.Application.Exceptions;
using FPTU_ProposalGuard.Application.Services.IExternalServices;
using FPTU_ProposalGuard.Domain.Common.Enums;
using FPTU_ProposalGuard.Domain.Entities;
using FPTU_ProposalGuard.Domain.Interfaces;
using FPTU_ProposalGuard.Domain.Interfaces.Repositories;
using FPTU_ProposalGuard.Domain.Interfaces.Services;
using FPTU_ProposalGuard.Domain.Interfaces.Services.Base;
using FPTU_ProposalGuard.Domain.Specifications;
using FPTU_ProposalGuard.Domain.Specifications.Interfaces;
using FPTU_ProposalGuard.Domain.Specifications.Params;
using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace FPTU_ProposalGuard.Application.Services;

public class ProjectProposalService(
    ISystemMessageService msgService,
    IUnitOfWork unitOfWork,
    IMapper mapper,
    IGenericRepository<ProjectProposal, int> projectProposalRepository,
    IS3Service s3Service,
    ILogger logger)
    : GenericService<ProjectProposal, ProjectProposalDto, int>(msgService, unitOfWork, mapper, logger),
        IProjectProposalService<ProjectProposalDto>
{
    private readonly IGenericRepository<ProjectProposal, int> _projectProposalRepository =
        unitOfWork.Repository<ProjectProposal, int>();

    public async Task<IServiceResult> CreateManyAsync(List<ProjectProposalDto> dtos)
    {
        if (dtos.Count == 0)
        {
            return new ServiceResult(ResultCodeConst.SYS_Fail0001,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0001));
        }

        try
        {
            var entities = _mapper.Map<List<ProjectProposal>>(dtos);

            await _unitOfWork.Repository<ProjectProposal, int>().AddRangeAsync(entities);
            await _unitOfWork.SaveChangesWithTransactionAsync();

            var returnDtos = _mapper.Map<List<ProjectProposalDto>>(entities);

            return new ServiceResult(ResultCodeConst.SYS_Success0001,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0001), returnDtos);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to create project proposals.");
            return new ServiceResult(ResultCodeConst.SYS_Fail0001,
                "Lỗi khi tạo Project Proposals: " + ex.Message);
        }
    }

    public override async Task<IServiceResult> GetByIdAsync(int id)
    {
        var baseSpec = new BaseSpecification<ProjectProposal>(pp => pp.ProjectProposalId == id);

        // Apply include
        baseSpec.ApplyInclude(q =>
                q.Include(pp => pp.ProposalHistories)
                    .ThenInclude(h => h.SimilarProposals)
                    .ThenInclude(s => s.MatchedSegments)
                    .Include(pp => pp.ProposalSupervisors!)
                    .Include(pp => pp.ProposalHistories).ThenInclude(h => h.SimilarProposals)
                    .ThenInclude(s => s.ExistingProposal)
                    .Include(pp => pp.ProposalStudents!))
            ;

        var entity = await _unitOfWork.Repository<ProjectProposal, int>().GetWithSpecAsync(baseSpec);
        if (entity == null)
        {
            return new ServiceResult(ResultCodeConst.SYS_Warning0004,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004));
        }

        var resultData = _mapper.Map<ProjectProposalDto>(entity);
        foreach (var history in resultData.ProposalHistories)
        {
            history.Url = (await s3Service.GetFileUrl(history.Url))!;
        }

        return new ServiceResult(ResultCodeConst.SYS_Success0002,
            await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
            resultData);
    }

    public async Task<IServiceResult> UpdateAsync(int id, ProjectProposalDto dto)
    {
        // Initiate service result
        var serviceResult = new ServiceResult();

        try
        {
            // Retrieve the entity
            var existingEntity = await _unitOfWork.Repository<ProjectProposal, int>().GetByIdAsync(id);
            if (existingEntity == null)
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004);
                return new ServiceResult(ResultCodeConst.SYS_Warning0004, errMsg);
            }

            // Process add update entity
            // Map properties from dto to existingEntity
            var localConfig = new TypeAdapterConfig();

            localConfig.NewConfig<ProjectProposalDto, ProjectProposal>()
                .Map(dest => dest.FunctionalRequirements,
                    src => JsonSerializer.Serialize(src.FunctionalRequirements, (JsonSerializerOptions?)null))
                .Map(dest => dest.NonFunctionalRequirements,
                    src => JsonSerializer.Serialize(src.NonFunctionalRequirements, (JsonSerializerOptions?)null))
                .Map(dest => dest.TechnicalStack,
                    src => JsonSerializer.Serialize(src.TechnicalStack, (JsonSerializerOptions?)null))
                .Map(dest => dest.Tasks,
                    src => JsonSerializer.Serialize(src.Tasks, (JsonSerializerOptions?)null))
                .Ignore(dest => dest.ProposalHistories)
                .Ignore(dest => dest.ProposalStudents)
                .Ignore(dest => dest.ProposalSupervisors)
                .IgnoreNullValues(true);
            dto.Adapt(existingEntity, localConfig);

            // Progress update when all require passed
            await _unitOfWork.Repository<ProjectProposal, int>()
                .UpdateAsync(existingEntity);

            // Check if there are any differences between the original and the updated entity
            if (!_unitOfWork.Repository<ProjectProposal, int>().HasChanges(existingEntity))
            {
                serviceResult.ResultCode = ResultCodeConst.SYS_Success0003;
                serviceResult.Message = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003);
                serviceResult.Data = true;
                return serviceResult;
            }

            // Save changes to DB
            var rowsAffected = await _unitOfWork.SaveChangesAsync();
            if (rowsAffected == 0)
            {
                serviceResult.ResultCode = ResultCodeConst.SYS_Fail0003;
                serviceResult.Message = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0003);
                serviceResult.Data = false;
            }

            // Mark as update success
            serviceResult.ResultCode = ResultCodeConst.SYS_Success0003;
            serviceResult.Message = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003);
            serviceResult.Data = true;
        }
        catch (UnprocessableEntityException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw;
        }

        return serviceResult;
    }

    public async Task<IServiceResult> UpdateStatus(int id, bool isApproved)
    {
        // Initiate service result
        var serviceResult = new ServiceResult();
        try
        {
            // Retrieve the entity
            var existingEntity = await _unitOfWork.Repository<ProjectProposal, int>().GetByIdAsync(id);
            if (existingEntity == null)
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004);
                return new ServiceResult(ResultCodeConst.SYS_Warning0004, errMsg);
            }

            existingEntity.Status = isApproved
                ? ProjectProposalStatus.Approved
                : ProjectProposalStatus.Rejected;

            // var latestHistory = existingEntity.ProposalHistories.MaxBy(h => h.Version);
            // latestHistory!.Status = isApproved
            //     ? ProjectProposalStatus.Approved.ToString()
            //     : ProjectProposalStatus.Rejected.ToString();

            await _unitOfWork.Repository<ProjectProposal, int>()
                .UpdateAsync(existingEntity);

            // Check if there are any differences between the original and the updated entity
            if (!_unitOfWork.Repository<ProjectProposal, int>().HasChanges(existingEntity))
            {
                serviceResult.ResultCode = ResultCodeConst.SYS_Success0003;
                serviceResult.Message = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003);
                serviceResult.Data = true;
                return serviceResult;
            }

            // Save changes to DB
            var rowsAffected = await _unitOfWork.SaveChangesAsync();
            if (rowsAffected == 0)
            {
                serviceResult.ResultCode = ResultCodeConst.SYS_Fail0003;
                serviceResult.Message = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0003);
                serviceResult.Data = false;
            }

            // Mark as update success
            serviceResult.ResultCode = ResultCodeConst.SYS_Success0003;
            serviceResult.Message = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003);
            serviceResult.Data = true;
        }
        catch (UnprocessableEntityException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw;
        }

        return serviceResult;
    }

    public override async Task<IServiceResult> GetAllWithSpecAsync(ISpecification<ProjectProposal> specification,
        bool tracked = true)
    {
        try
        {
            var proposalSpec = specification as ProposalSpecification;
            if (proposalSpec == null)
            {
                return new ServiceResult(ResultCodeConst.SYS_Fail0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0002));
            }

            // Count total proposals
            var totalCount = await _projectProposalRepository.CountAsync(specification);
            // Count total page
            var totalPage = (int)Math.Ceiling((double)totalCount / proposalSpec.PageSize);

            // Set pagination to specification after count total proposal 
            if (proposalSpec.PageIndex > totalPage
                || proposalSpec.PageIndex < 1) // Exceed total page or page index smaller than 1
            {
                proposalSpec.PageIndex = 1; // Set default to first page
            }

            // Apply pagination
            proposalSpec.ApplyPaging(
                skip: proposalSpec.PageSize * (proposalSpec.PageIndex - 1),
                take: proposalSpec.PageSize);

            var entities = await _unitOfWork.Repository<ProjectProposal, int>()
                .GetAllWithSpecAsync(specification, tracked);

            if (entities.Any())
            {
                // Convert to dto collection 
                var proposalDtos = _mapper.Map<IEnumerable<ProjectProposalDto>>(entities).ToList();

                // change all proposal.history.url to exact url through s3 service
                foreach (var proposalDto in proposalDtos)
                {
                    if (proposalDto.ProposalHistories != null)
                    {
                        foreach (var history in proposalDto.ProposalHistories)
                        {
                            history.Url = (await s3Service.GetFileUrl(history.Url))!;
                        }
                    }
                }

                // Pagination result 
                var paginationResultDto = new PaginatedResultDto<ProjectProposalDto>(proposalDtos,
                    proposalSpec.PageIndex, proposalSpec.PageSize, totalPage, totalCount);

                // Response with pagination
                return new ServiceResult(ResultCodeConst.SYS_Success0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), paginationResultDto);
            }
            // Not found any data

            return new ServiceResult(ResultCodeConst.SYS_Warning0004,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004),
                // Mapping entities to dto and ignore sensitive user data
                _mapper.Map<IEnumerable<ProjectProposalDto>>(entities).ToList());
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when progress get all data");
        }
    }
}