using System.ComponentModel;
using System.Reflection;
using FPTU_ProposalGuard.Domain.Common.Enums;
using FPTU_ProposalGuard.Domain.Entities;
using FPTU_ProposalGuard.Domain.Interfaces;
using FPTU_ProposalGuard.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace FPTU_ProposalGuard.Infrastructure.Data;

public class DatabaseInitializer(FptuProposalGuardDbContext context, ILogger logger) : IDatabaseInitializer
{
    public async Task InitializeAsync()
    {
        try
        {
            // Check whether the database exists and can be connected to
            if (!await context.Database.CanConnectAsync())
            {
                // Check for applied migrations
                var appliedMigrations = await context.Database.GetAppliedMigrationsAsync();
                if (appliedMigrations.Any())
                {
                    logger.Information("Migrations have been applied.");
                    return;
                }

                // Perform migration if necessary
                await context.Database.MigrateAsync();
                logger.Information("Database initialized successfully.");
            }
            else
            {
                logger.Information("Database cannot be connected to.");
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "An error occurred while initializing the database.");
        }
    }

    public async Task SeedAsync()
    {
        try
        {
            await TrySeedAsync();
        }
        catch (Exception ex)
        {
            logger.Error(ex, "An error occurred while seeding the database.");
        }
    }

    public async Task TrySeedAsync()
    {
        try
        {
            if (!await context.Users.AnyAsync()) await SeedUserRoleAsync();
            if (!await context.Semesters.AnyAsync()) await SeedSemesterAsync();
            if (!await context.ReviewQuestions.AnyAsync()) await SeedQuestionsAsync();
            // if (!await context.ReviewSessions.AnyAsync()) await SeedReviewSessionsAsync();
        }
        catch (Exception ex)
        {
            logger.Error(ex, "An error occurred while performing seed data.");
        }
    }

    private async Task SeedUserRoleAsync()
    {
        List<SystemRole> roles = new()
        {
            new()
            {
                RoleName = nameof(Role.Administration),
                NormalizedName = nameof(Role.Administration).ToUpper(),
                Description = Role.Administration.GetDescription()
            },
            new()
            {
                RoleName = nameof(Role.Reviewer),
                NormalizedName = nameof(Role.Reviewer).ToUpper(),
                Description = Role.Reviewer.GetDescription()
            },
            new()
            {
                RoleName = nameof(Role.Lecturer),
                NormalizedName = nameof(Role.Lecturer).ToUpper(),
                Description = Role.Lecturer.GetDescription()
            },
            new()
            {
                RoleName = nameof(Role.Moderator),
                NormalizedName = nameof(Role.Lecturer).ToUpper(),
                Description = Role.Moderator.GetDescription()
            }
        };
        // Add range
        await context.AddRangeAsync(roles);
        // Save DB
        var isSaved = await context.SaveChangesAsync() > 0;
        if (isSaved) logger.Information($"[ROLE] Seed {roles} data successfully");
        else return;

        List<User> users = new()
        {
            new()
            {
                Email = "admin@gmail.com",
                FirstName = "Admin",
                PasswordHash = BC.EnhancedHashPassword("@Admin123", 13),
                IsActive = true,
                EmailConfirmed = true,
                IsDeleted = false,
                CreateDate = DateTime.UtcNow,
                TwoFactorEnabled = false,
                RoleId = roles.First(r => r.RoleName == nameof(Role.Administration)).RoleId
            },
            new()
            {
                Email = "doanvietthanhhs@gmail.com",
                FirstName = "Chu",
                LastName = "Be",
                PasswordHash = BC.EnhancedHashPassword("@Admin123", 13),
                IsActive = true,
                EmailConfirmed = true,
                IsDeleted = false,
                CreateDate = DateTime.UtcNow,
                TwoFactorEnabled = false,
                RoleId = roles.First(r => r.RoleName == nameof(Role.Administration)).RoleId
            },
            new()
            {
                Email = "moderator@gmail.com",
                FirstName = "Moderator",
                PasswordHash = BC.EnhancedHashPassword("@Moderator123", 13),
                IsActive = true,
                EmailConfirmed = true,
                IsDeleted = false,
                CreateDate = DateTime.UtcNow,
                TwoFactorEnabled = false,
                RoleId = roles.First(r => r.RoleName == nameof(Role.Moderator)).RoleId
            },
            new()
            {
                Email = "Lecturer1@gmail.com",
                FirstName = "Lecturer1",
                PasswordHash = BC.EnhancedHashPassword("@Lecturer123", 13),
                IsActive = true,
                EmailConfirmed = true,
                IsDeleted = false,
                CreateDate = DateTime.UtcNow,
                TwoFactorEnabled = false,
                RoleId = roles.First(r => r.RoleName == nameof(Role.Lecturer)).RoleId
            },
            new()
            {
                Email = "Lecturer2@gmail.com",
                FirstName = "Lecturer2",
                PasswordHash = BC.EnhancedHashPassword("@Lecturer123", 13),
                IsActive = true,
                EmailConfirmed = true,
                IsDeleted = false,
                CreateDate = DateTime.UtcNow,
                TwoFactorEnabled = false,
                RoleId = roles.First(r => r.RoleName == nameof(Role.Lecturer)).RoleId
            }
        };

        // Add range
        await context.AddRangeAsync(users);
        // Save change
        isSaved = await context.SaveChangesAsync() > 0;
        if (isSaved) logger.Information($"[USER] Seed {users} data successfully");
    }

    private async Task SeedSemesterAsync()
    {
        List<Semester> semesters = new()
        {
            new()
            {
                Term = Term.Spring,
                Year = 2025,
                SemesterCode = "SP25"
            },
            new()
            {
                Term = Term.Summer,
                Year = 2025,
                SemesterCode = "SU25"
            },
            new()
            {
                Term = Term.Fall,
                Year = 2025,
                SemesterCode = "    "
            }
        };

        // Add range
        await context.AddRangeAsync(semesters);
        // Save change
        var isSaved = await context.SaveChangesAsync() > 0;
        if (isSaved) logger.Information($"[SEMESTER] Seed {semesters} data successfully");
    }

    private async Task SeedQuestionsAsync()
    {
        List<ReviewQuestion> reviewQuestions = new()
        {
            new()
            {
                AnswerType = AnswerType.YesNo,
                QuestionContent =
                    "Tên đề tài (project title) có phản ánh được định hướng thực hiện nghiên cứu và phát triển sản phẩm của nhóm SV?"
            },
            new()
            {
                AnswerType = AnswerType.YesNo,
                QuestionContent = "Ngữ cảnh (context) nơi sản phẩm được triển khai có được xác định cụ thể?"
            },
            new()
            {
                AnswerType = AnswerType.YesNo,
                QuestionContent =
                    "Vấn đề cần giải quyết (problem statement) có được mô tả rõ ràng là động lực cho việc ra đời của sản phẩm?"
            },
            new()
            {
                AnswerType = AnswerType.YesNo,
                QuestionContent = "Người dùng chính (main actors) có được xác định trong đề tài?"
            },
            new()
            {
                AnswerType = AnswerType.YesNo,
                QuestionContent =
                    "Các luồng xử lý chính (main flows) và các chức năng chính (main usescases)  của người dùng có được mô tả?"
            },
            new()
            {
                AnswerType = AnswerType.YesNo,
                QuestionContent = "Khách hàng/người tài trợ (customers/sponsors) của đề tài có được xác định?"
            },
            new()
            {
                AnswerType = AnswerType.YesNo,
                QuestionContent =
                    "Hướng tiếp cận (Approach) về lý thuyết (theory),công nghệ áp dụng (applied technology)   và các sản phẩm cần tạo ra trong đề tài (main deliverables) có được xác định và phù hợp?"
            },
            new()
            {
                AnswerType = AnswerType.YesNo,
                QuestionContent =
                    "Phạm vi đề tài (Scope) và độ lớn của sản phẩm (size of product) có khả thi và phù hợp cho nhóm (3-5) SV thực hiện trong 14 tuần ? Có phân chia thành các gói packages để đánh giá?"
            },
            new()
            {
                AnswerType = AnswerType.YesNo,
                QuestionContent =
                    "Độ phức tạp và tính kỹ thuật (Complexity/technicality) củ đề là phù hợp với yêu cầu năng lực của 1 đề tài Capstone project cho ngành Kỹ thuật phần mềm?"
            },
            new()
            {
                AnswerType = AnswerType.YesNo,
                QuestionContent =
                    "Đề tài xây dựng hướng đến việc giải quyết các vấn đề thực tế (Applicability) và khả thi về mặt công nghệ (technologically feasible) trong giới hạn thời gian của dự án?"
            }
        };

        // Add range
        await context.AddRangeAsync(reviewQuestions);
        // Save change
        var isSaved = await context.SaveChangesAsync() > 0;
        if (isSaved) logger.Information($"[REVIEW_QUESTION] Seed {reviewQuestions} data successfully");
    }
    
    private async Task SeedReviewSessionsAsync()
    {
        List<ReviewSession> reviewQuestions = new()
        {
             new ReviewSession()
             {
                 HistoryId = 1,
                 ReviewStatus = ReviewStatus.Pending,
                 ReviewerId = new Guid("484E02FB-4A7F-F011-9ECB-D03C1F563019")
             }
        };

        // Add range
        await context.AddRangeAsync(reviewQuestions);
        // Save change
        var isSaved = await context.SaveChangesAsync() > 0;
        if (isSaved) logger.Information($"[REVIEW_QUESTION] Seed {reviewQuestions} data successfully");
    }
}

public static class DatabaseInitializerExtensions
{
    public static string GetDescription(this System.Enum value)
    {
        var field = value.GetType().GetField(value.ToString());
        var attribute = field?.GetCustomAttribute<DescriptionAttribute>();

        return attribute?.Description ?? value.ToString();
    }
}