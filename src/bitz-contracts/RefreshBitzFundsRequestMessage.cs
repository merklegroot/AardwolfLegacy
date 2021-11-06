using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bitz_contracts
{
    public class RefreshBitzFundsRequestMessage
    {
        public Guid Id { get; set; }
        public string RequestingMachine = Environment.MachineName;
        public DateTime TimeStampUtc { get; set; }
    }
}
