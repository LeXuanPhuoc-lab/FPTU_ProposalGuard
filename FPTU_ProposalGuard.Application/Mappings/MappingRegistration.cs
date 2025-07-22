﻿using System.Text.Json;
using FPTU_ProposalGuard.Application.Dtos;
using FPTU_ProposalGuard.Application.Dtos.Notifications;
using FPTU_ProposalGuard.Application.Dtos.Proposals;
using FPTU_ProposalGuard.Application.Dtos.SystemRoles;
using FPTU_ProposalGuard.Application.Dtos.Users;
using FPTU_ProposalGuard.Domain.Entities;
using Mapster;

namespace FPTU_ProposalGuard.Application.Mappings;

public class MappingRegistration : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // From [Entity] to [Dto]
        config.NewConfig<User, UserDto>();
        config.NewConfig<Notification, NotificationDto>();
        config.NewConfig<SystemRole, SystemRoleDto>();
        config.NewConfig<SystemMessage, SystemMessageDto>();
        config.NewConfig<ProposalStudent, ProposalStudentDto>();
        config.NewConfig<ProposalSupervisor, ProposalSupervisorDto>();
        config.NewConfig<ProposalHistory, ProposalHistoryDto>();
        config.NewConfig<ProposalSimilarity, ProposalSimilarityDto>();
        config.NewConfig<ProposalMatchedSegment, ProposalMatchedSegmentDto>();

        config.NewConfig<ProjectProposal, ProjectProposalDto>()
            .Map(dest => dest.FunctionalRequirements,
                src => string.IsNullOrWhiteSpace(src.FunctionalRequirements)
                    ? new List<string>()
                    : JsonSerializer.Deserialize<List<string>>(src.FunctionalRequirements!,
                        (JsonSerializerOptions?)null)!)
            .Map(dest => dest.NonFunctionalRequirements,
                src => string.IsNullOrWhiteSpace(src.NonFunctionalRequirements)
                    ? new List<string>()
                    : JsonSerializer.Deserialize<List<string>>(src.NonFunctionalRequirements!,
                        (JsonSerializerOptions?)null)!)
            .Map(dest => dest.TechnicalStack,
                src => string.IsNullOrWhiteSpace(src.TechnicalStack)
                    ? new List<string>()
                    : JsonSerializer.Deserialize<List<string>>(src.TechnicalStack!, (JsonSerializerOptions?)null)!)
            .Map(dest => dest.Tasks,
                src => string.IsNullOrWhiteSpace(src.Tasks)
                    ? new List<string>()
                    : JsonSerializer.Deserialize<List<string>>(src.Tasks!, (JsonSerializerOptions?)null)!);
        ;

        // From [Dto] to [Entity]
        config.NewConfig<ProjectProposalDto, ProjectProposal>()
            .Map(dest => dest.FunctionalRequirements,
                src => JsonSerializer.Serialize(src.FunctionalRequirements, (JsonSerializerOptions?)null))
            .Map(dest => dest.NonFunctionalRequirements,
                src => JsonSerializer.Serialize(src.NonFunctionalRequirements, (JsonSerializerOptions?)null))
            .Map(dest => dest.TechnicalStack,
                src => JsonSerializer.Serialize(src.TechnicalStack, (JsonSerializerOptions?)null))
            .Map(dest => dest.Tasks,
                src => JsonSerializer.Serialize(src.Tasks, (JsonSerializerOptions?)null))
            .IgnoreNullValues(true);
        config.NewConfig<ProposalStudentDto, ProposalStudent>()
            .IgnoreNullValues(true);
        config.NewConfig<ProposalSupervisorDto, ProposalSupervisor>()
            .IgnoreNullValues(true);
    }
}