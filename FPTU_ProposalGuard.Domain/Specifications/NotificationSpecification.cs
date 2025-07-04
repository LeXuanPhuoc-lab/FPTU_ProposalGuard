﻿using System.Linq.Expressions;
using FPTU_ProposalGuard.Domain.Entities;
using FPTU_ProposalGuard.Domain.Specifications.Params;
using Microsoft.EntityFrameworkCore;

namespace FPTU_ProposalGuard.Domain.Specifications;

public class NotificationSpecification : BaseSpecification<Notification>
{
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
    public string? Email { get; set; }
    public bool IsCallFromManagement { get; set; }

    public NotificationSpecification(NotificationSpecParams specParams, int pageIndex, int pageSize,
        bool isCallFromManagement = false) 
        : base(n =>
        string.IsNullOrEmpty(specParams.Search) ||
        (
            (!string.IsNullOrEmpty(n.Title) && n.Title.Contains(specParams.Search)) ||
            (!string.IsNullOrEmpty(n.Message) && n.Message.Contains(specParams.Search)) ||
            
            // Notification recipient info
            n.NotificationRecipients.Any(nr => 
                !string.IsNullOrEmpty(nr.Recipient.Email) && nr.Recipient.Email.Contains(specParams.Search) ||
                !string.IsNullOrEmpty(nr.Recipient.FirstName) && nr.Recipient.FirstName.Contains(specParams.Search) ||
                !string.IsNullOrEmpty(nr.Recipient.LastName) && nr.Recipient.LastName.Contains(specParams.Search)
            )
        ))
    {
        // Assign email
        Email = specParams.Email;
        
        // Assign bool
        IsCallFromManagement = isCallFromManagement;
        
        // Pagination
        PageIndex = pageIndex;
        PageSize = pageSize;

        // Enable split query
        EnableSplitQuery();

        // Filter for specific usage (privacy or management)
        if (!string.IsNullOrEmpty(specParams.Email))
        {
            // Check whether is call from management
            if (!isCallFromManagement) // Used by user
            {
                AddFilter(s => 
                    s.IsPublic == true || // All with public  
                    (
                        // Combined with privacy of their email
                        s.IsPublic == false && s.NotificationRecipients.Any(r => r.Recipient.Email == specParams.Email)
                    )
                );
                
                // Check whether filtering private field
                if(specParams.isPublic !=  null) AddFilter(s => s.IsPublic == specParams.isPublic);
            }
            else if (isCallFromManagement) // Called by employee
            {
                // Only process get all user's privacy notification
                AddFilter(s => s.IsPublic == false && s.NotificationRecipients.Any(r => r.Recipient.Email == specParams.Email));
            }
        }
        else if (string.IsNullOrEmpty(specParams.Email) && isCallFromManagement)
        {
            // Filter default as public only
            AddFilter(s => s.IsPublic == true);
        }

        if (specParams.CreatedBy != null) // Created by
        {
            AddFilter(x => x.CreatedBy.Email == specParams.CreatedBy);
        }
        if (specParams.NotificationType != null) // Notification type
        {
            AddFilter(x => x.Type == specParams.NotificationType);
        }
        
        if (specParams.CreateDateRange != null
            && specParams.CreateDateRange.Length > 1) // With range of create date 
        {
            if (specParams.CreateDateRange[0].HasValue && specParams.CreateDateRange[1].HasValue)
            {
                AddFilter(x =>
                    x.CreateDate >= specParams.CreateDateRange[0]!.Value.Date
                    && x.CreateDate <= specParams.CreateDateRange[1]!.Value.Date);
            }
            else if (specParams.CreateDateRange[0] is null && specParams.CreateDateRange[1].HasValue)
            {
                AddFilter(x => x.CreateDate <= specParams.CreateDateRange[1]);
            }
            else if (specParams.CreateDateRange[0].HasValue && specParams.CreateDateRange[1] is null)
            {
                AddFilter(x => x.CreateDate >= specParams.CreateDateRange[0]);
            }
        }

        if (!string.IsNullOrEmpty(specParams.Sort))
        {
            var sortBy = specParams.Sort.Trim();
            var isDescending = sortBy.StartsWith("-");
            var propertyName = isDescending ? sortBy.Substring(1) : sortBy;

            ApplySorting(propertyName, isDescending);
        }
        else
        {
            AddOrderByDescending(n => n.CreateDate);
        }
    }

    private void ApplySorting(string propertyName, bool isDescending)
    {
        if (string.IsNullOrEmpty(propertyName)) return;

        // Use Reflection to dynamically apply sorting
        var parameter = Expression.Parameter(typeof(Notification), "x");
        var property = Expression.Property(parameter, propertyName);
        var sortExpression =
            Expression.Lambda<Func<Notification, object>>(Expression.Convert(property, typeof(object)), parameter);

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