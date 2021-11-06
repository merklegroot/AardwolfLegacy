using MongoDB.Bson;
using System;

namespace coss_browser_service_lib.Models
{
    public class CookieContainerEntity
    {
        public ObjectId Id { get; set;  }
        public DateTime TimeStampUtc { get; set; }
        public string SessionToken { get; set; }
        public string XsrfToken { get; set; }        
    }
}
