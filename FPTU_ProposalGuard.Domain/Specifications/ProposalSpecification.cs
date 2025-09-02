using System.Linq.Expressions;
using FPTU_ProposalGuard.Domain.Common.Enums;
using FPTU_ProposalGuard.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FPTU_ProposalGuard.Domain.Specifications;

public class ProposalSpecification : BaseSpecification<ProjectProposal>
{
    public int PageIndex { get; set; }
    public int PageSize { get; set; }


    public ProposalSpecification(ProposalSpecParams specParams, int pageIndex, int pageSize, string? createdBy = null)
        : base(p =>
            (string.IsNullOrEmpty(specParams.Search) ||
             (
                 // VieTitle
                 (!string.IsNullOrEmpty(p.VieTitle) && p.VieTitle.Contains(specParams.Search)) ||
                 // EngTitle
                 (!string.IsNullOrEmpty(p.EngTitle) && p.EngTitle.Contains(specParams.Search)) ||
                 // Abbreviation
                 (!string.IsNullOrEmpty(p.Abbreviation) && p.Abbreviation.Contains(specParams.Search))
             )) && (string.IsNullOrEmpty(createdBy) || p.CreatedBy == createdBy)
        )

    {
        // Pagination
        PageIndex = pageIndex;
        PageSize = pageSize;


        // Enable split query
        EnableSplitQuery();

        // Include related entities
        ApplyInclude(q => q.Include(p => p.ProposalSupervisors!)
            .Include(p => p.ProposalStudents!)
            .Include(p => p.Semester)
            .Include(p => p.ProposalHistories.OrderByDescending(h => h.Version))
            .ThenInclude(p => p.ReviewSessions)
        );

        //Default order by created date
        AddOrderBy(p => p.CreatedAt);

        if (!string.IsNullOrEmpty(specParams.Title))
        {
            AddFilter(p => p.VieTitle.Contains(specParams.Title) || p.EngTitle.Contains(specParams.Title));
        }

        // Filter by Abbreviation
        if (!string.IsNullOrEmpty(specParams.Abbreviation))
        {
            AddFilter(p => p.Abbreviation.Contains(specParams.Abbreviation));
        }

        // Filter by Semester
        if (!string.IsNullOrEmpty(specParams.SemesterCode))
        {
            AddFilter(p => p.Semester!.SemesterCode.Contains(specParams.SemesterCode));
        }

        if (specParams.CreateDateRange != null
            && specParams.CreateDateRange.Length > 1) // With range of creation date
        {
            if (specParams.CreateDateRange[0].HasValue && specParams.CreateDateRange[1].HasValue)
            {
                AddFilter(x => x.CreatedAt.Date >= specParams.CreateDateRange[0]!.Value.Date &&
                               x.CreatedAt.Date <= specParams.CreateDateRange[1]!.Value.Date);
            }
            else if ((specParams.CreateDateRange[0] is null && specParams.CreateDateRange[1].HasValue))
            {
                AddFilter(x => x.CreatedAt <= specParams.CreateDateRange[1]);
            }
            else if (specParams.CreateDateRange[0].HasValue && specParams.CreateDateRange[1] is null)
            {
                AddFilter(x => x.CreatedAt >= specParams.CreateDateRange[0]);
            }
        }

        // Filter by DurationFrom
        if (specParams.DurationFromRange != null
            && specParams.DurationFromRange.Length > 1) // With range of creation date
        {
            if (specParams.DurationFromRange[0].HasValue && specParams.DurationFromRange[1].HasValue)
            {
                AddFilter(x => x.DurationFrom >= specParams.DurationFromRange[0]!.Value &&
                               x.DurationFrom <= specParams.DurationFromRange[1]!.Value);
            }
            else if ((specParams.DurationFromRange[0] is null && specParams.DurationFromRange[1].HasValue))
            {
                AddFilter(x => x.DurationFrom <= specParams.DurationFromRange[1]);
            }
            else if (specParams.DurationFromRange[0].HasValue && specParams.DurationFromRange[1] is null)
            {
                AddFilter(x => x.DurationFrom >= specParams.DurationFromRange[0]);
            }
        }

        // Filter by DurationTo
        if (specParams.DurationToRange != null
            && specParams.DurationToRange.Length > 1) // With range of creation date
        {
            if (specParams.DurationToRange[0].HasValue && specParams.DurationToRange[1].HasValue)
            {
                AddFilter(x => x.DurationTo >= specParams.DurationToRange[0]!.Value &&
                               x.DurationTo <= specParams.DurationToRange[1]!.Value);
            }
            else if ((specParams.DurationToRange[0] is null && specParams.DurationToRange[1].HasValue))
            {
                AddFilter(x => x.DurationTo <= specParams.DurationToRange[1]);
            }
            else if (specParams.DurationToRange[0].HasValue && specParams.DurationToRange[1] is null)
            {
                AddFilter(x => x.DurationTo >= specParams.DurationToRange[0]);
            }
        }

        // Filter by Status
        if (!string.IsNullOrEmpty(specParams.Status))
        {
            if (Enum.TryParse<ProjectProposalStatus>(specParams.Status, true, out var status))
            {
                AddFilter(p => p.Status == status);
            }
        }

        // Filter by SupervisorName
        if (!string.IsNullOrEmpty(specParams.SupervisorName))
        {
            AddFilter(p => p.ProposalSupervisors!.Any(s => s.FullName.Contains(specParams.SupervisorName)));
        }

        // Filter by SupervisorEmail
        if (!string.IsNullOrEmpty(specParams.SupervisorEmail))
        {
            AddFilter(p => p.ProposalSupervisors!.Any(s => s.Email.Contains(specParams.SupervisorName)));
        }

        // Filter by StudentName
        if (!string.IsNullOrEmpty(specParams.StudentName))
        {
            AddFilter(p => p.ProposalStudents!.Any(s => s.FullName.Contains(specParams.StudentName)));
        }

        // Filter by StudentCode
        if (!string.IsNullOrEmpty(specParams.StudentCode))
        {
            AddFilter(p => p.ProposalStudents!.Any(s => s.StudentCode.Contains(specParams.StudentCode)));
        }

        // Filter by StudentEmail
        if (!string.IsNullOrEmpty(specParams.StudentEmail))
        {
            AddFilter(p => p.ProposalStudents!.Any(s => s.Email.Contains(specParams.StudentEmail)));
        }

        // Apply Sorting
        if (!string.IsNullOrEmpty(specParams.Sort))
        {
            var sortBy = specParams.Sort.Trim();
            var isDescending = sortBy.StartsWith("-");
            var propertyName = isDescending ? sortBy.Substring(1) : sortBy;

            ApplySorting(propertyName, isDescending);
        }
        else
        {
            // Default order by create date
            AddOrderByDescending(u => u.CreatedAt);
        }
    }

    private void ApplySorting(string propertyName, bool isDescending)
    {
        if (string.IsNullOrEmpty(propertyName)) return;

        // Use Reflection to dynamically apply sorting
        var parameter = Expression.Parameter(typeof(ProjectProposal), "x");
        var property = Expression.Property(parameter, propertyName);
        var sortExpression =
            Expression.Lambda<Func<ProjectProposal, object>>(Expression.Convert(property, typeof(object)), parameter);

        if (isDescending)
        {
            AddOrderByDescending(sortExpression);
        }
        else
        {
            AddOrderBy(sortExpression);
        }
    }
}