﻿namespace FPTU_ProposalGuard.Application.Dtos;

public class PaginatedResultDto<T> 
{
    public IEnumerable<T> Sources { get; set; }
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
    public int TotalPage { get; set; }
    public int TotalActualItem { get; set; }

    public PaginatedResultDto(IEnumerable<T> sources, int pageIndex, int pageSize, int totalPage, int totalActualItem)
    {
        Sources = sources;
        PageIndex = pageIndex;
        PageSize = pageSize;
        TotalPage = totalPage;
        TotalActualItem = totalActualItem;
    }
}