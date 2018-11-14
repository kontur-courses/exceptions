using System;
using System.Collections.Generic;
// ReSharper disable CollectionNeverQueried.Local
// ReSharper disable FieldCanBeMadeReadOnly.Local

namespace Exceptions
{
    // Что тут не так?
    public class ReportReceiver
    {
        private Dictionary<Guid, Report> reportsCache 
            = new Dictionary<Guid, Report>();
        private Dictionary<Guid, Organization> sendersCache 
            = new Dictionary<Guid, Organization>();
        private Dictionary<Guid, Organization> receiversCache 
            = new Dictionary<Guid, Organization>();
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
        public Organization Get(Guid senderId)
        {
            throw new NotImplementedException();
        }
    }

    public class Organization
    {
    }

    public class Report
    {
        public Guid Id { get; private set; }
        public Guid SenderId { get; private set; }
        public Guid ReceiverId { get; private set; }
    }
}