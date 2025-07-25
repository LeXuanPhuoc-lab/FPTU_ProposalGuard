﻿using FPTU_ProposalGuard.Domain.Common.Enums;

namespace FPTU_ProposalGuard.API.Payloads.Requests.Notifications;

public class CreateNotificationRequest
{
    public string Title { get; set; } = null!;
    public string Message { get; set; } = null!;
    public bool IsPublic { get; set; }
    public NotificationType NotificationType { get; set; } 
    public List<string>? ListRecipient { get; set; }
}