﻿namespace FPTU_ProposalGuard.Application.Exceptions
{
	[Serializable]
	public class BadRequestException : Exception
	{
        public BadRequestException() { }
       
        public BadRequestException(string message) : base(message)
        {
        }
    }
}
