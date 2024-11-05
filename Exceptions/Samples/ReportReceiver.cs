using System;
using System.Collections.Generic;
// ReSharper disable CollectionNeverQueried.Local
// ReSharper disable FieldCanBeMadeReadOnly.Local
#pragma warning disable IDE0044 // Add readonly modifier

namespace Exceptions.Samples;

// Что тут не так?
public class ReportReceiver
{
    private Dictionary<Guid, Report> reportsCache = new();
    private Dictionary<Guid, Organization> sendersCache = new();
    private Dictionary<Guid, Organization> receiversCache = new();
    private readonly OrganizationRepo organizationsRepo;

    public ReportReceiver(OrganizationRepo organizationsRepo)
    {
        this.organizationsRepo = organizationsRepo;
    }

    public void ReceiveReport(Report report)
    {
        if (!reportsCache.ContainsKey(report.Id))
        {
            sendersCache.Add(report.Id, organizationsRepo.Get(report.SenderId));
            receiversCache.Add(report.Id, organizationsRepo.Get(report.ReceiverId));
            reportsCache.Add(report.Id, report);
        }
    }

    //...
}


public class OrganizationRepo
{
    public Organization Get(Guid senderId) => throw new NotImplementedException();
}

public class Organization
{
}

public record Report(Guid Id, Guid SenderId, Guid ReceiverId);