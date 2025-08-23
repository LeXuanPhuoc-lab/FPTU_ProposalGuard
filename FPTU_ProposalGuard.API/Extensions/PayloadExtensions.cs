using System.Runtime.InteropServices.JavaScript;
using FPTU_ProposalGuard.API.Payloads.Requests;
using FPTU_ProposalGuard.API.Payloads.Requests.Authentications;
using FPTU_ProposalGuard.API.Payloads.Requests.Notifications;
using FPTU_ProposalGuard.API.Payloads.Requests.Proposals;
using FPTU_ProposalGuard.API.Payloads.Requests.Users;
using FPTU_ProposalGuard.Application.Dtos;
using FPTU_ProposalGuard.Application.Dtos.Notifications;
using FPTU_ProposalGuard.Application.Dtos.Proposals;
using FPTU_ProposalGuard.Application.Dtos.Reviews;
using FPTU_ProposalGuard.Application.Dtos.Users;
using FPTU_ProposalGuard.Domain.Common.Enums;

namespace FPTU_ProposalGuard.API.Extensions;

// Summary:
//		This class provide extensions method mapping from request payload to specific 
//		application objects
public static class PayloadExtensions
{
    #region Notifications

    public static NotificationDto ToNotificationDto(this CreateNotificationRequest req)
        => new()
        {
            Title = req.Title,
            Message = req.Message,
            Type = req.NotificationType,
            IsPublic = req.IsPublic
        };

    public static NotificationDto ToNotificationDto(this UpdateNotificationRequest req)
        => new()
        {
            Title = req.Title,
            Message = req.Message,
            Type = req.NotificationType
        };

    #endregion

    #region Users

    public static UserDto ToUserDto(this CreateUserRequest req)
    {
        return new()
        {
            FirstName = req.FirstName,
            LastName = req.LastName,
            Dob = req.Dob,
            Phone = req.Phone,
            Address = req.Address,
            Gender = req.Gender.ToString(),
            RoleId = req.RoleId
        };
    }

    public static UserDto ToUserDto(this UpdateUserRequest req)
    {
        return new()
        {
            FirstName = req.FirstName,
            LastName = req.LastName,
            Dob = req.Dob,
            Phone = req.Phone,
            Address = req.Address,
            Gender = req.Gender.ToString(),
            Avatar = req.Avatar
        };
    }

    public static UserDto ToUserDto(this UpdateProfileRequest req, string email)
    {
        return new()
        {
            Email = email,
            FirstName = req.FirstName,
            LastName = req.LastName,
            Dob = req.Dob,
            Phone = req.Phone,
            Address = req.Address,
            Gender = req.Gender.ToString(),
            Avatar = req.Avatar
        };
    }

    #endregion

    #region Proposal

    public static List<(IFormFile files, ProposalHistoryDto)> ToTupleList(this AddProposalsWithFilesRequest req)
    {
        var fileHistoryPairs = req.Files.Select(checkedFile =>
        {
            var file = checkedFile.File;
            var hasSimilarity = checkedFile.SimilarityDetails is
                { Count: > 0 }; // check null and if not it will count and check
            var history = new ProposalHistoryDto()
            {
                Status = ProjectProposalStatus.Pending.ToString(),
                Version = 1,
                Comment = null,
                SimilarProposals = hasSimilarity
                    ? checkedFile.SimilarityDetails!.Select(similarity =>
                    {
                        var similarityDetail = new ProposalSimilarityDto()
                        {
                            ExistingProposalId = similarity.SimilarProposalId,
                            MatchCount = similarity.MatchCount,
                            MatchRatio = similarity.MatchRatio,
                            LongestSequence = similarity.LongestContiguous,
                            OverallScore = (decimal)similarity.OverallScore,
                            MatchedSegments = similarity.Segments.Select(s => new ProposalMatchedSegmentDto
                            {
                                Context = s.Text,
                                MatchContext = s.UploadedChunkText,
                                MatchPercentage = s.Score
                            }).ToList()
                        };
                        foreach (var segment in similarityDetail.MatchedSegments)
                        {
                            segment.ProposalSimilarity = similarityDetail;
                        }

                        return similarityDetail;
                    }).ToList()
                    : new List<ProposalSimilarityDto>()
            };
            foreach (var sim in history.SimilarProposals)
            {
                sim.ProposalHistory = history;
            }

            return (file, history);
        }).ToList();
        return fileHistoryPairs;
    }
    
    public static (IFormFile files, ProposalHistoryDto) ToTuple(this ReUploadRequest req)
    {
        var file = req.CheckedFileFile.File;
        var history = new ProposalHistoryDto()
        {
            Status = ProjectProposalStatus.Pending.ToString(),
            // Version = 1,
            Comment = null,
            SimilarProposals = req.CheckedFileFile.SimilarityDetails is
                { Count: > 0 }
                ? req.CheckedFileFile.SimilarityDetails!.Select(similarity =>
                {
                    var similarityDetail = new ProposalSimilarityDto()
                    {
                        ExistingProposalId = similarity.SimilarProposalId,
                        MatchCount = similarity.MatchCount,
                        MatchRatio = similarity.MatchRatio,
                        LongestSequence = similarity.LongestContiguous,
                        OverallScore = (decimal)similarity.OverallScore,
                        MatchedSegments = similarity.Segments.Select(s => new ProposalMatchedSegmentDto
                        {
                            Context = s.Text,
                            MatchContext = s.UploadedChunkText,
                            MatchPercentage = s.Score
                        }).ToList()
                    };
                    foreach (var segment in similarityDetail.MatchedSegments)
                    {
                        segment.ProposalSimilarity = similarityDetail;
                    }

                    return similarityDetail;
                }).ToList()
                : new List<ProposalSimilarityDto>()
        };
        foreach (var sim in history.SimilarProposals)
        {
            sim.ProposalHistory = history;
        }

        return (file, history);
    }

    public static ReviewSessionDto ToDto(this SubmitReviewRequest req)
    {
        return new ReviewSessionDto
        {
            Answers = req.SingleAnswers.Select(a => new ReviewAnswerDto
            {
                QuestionId = a.QuestionId,
                Answer = a.Answer
            }).ToList(),
            Comment = req.Comment,
            ReviewStatus = Enum.Parse<ReviewStatus>(req.ReviewStatus)
        };
    }

    #endregion
}