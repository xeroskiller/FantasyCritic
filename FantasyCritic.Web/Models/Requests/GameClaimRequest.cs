using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FantasyCritic.Web.Models.Requests
{
    public class ClaimGameRequest
    {
        public Guid PublisherID { get; set; }
        public string GameName { get; set; }
        public bool CounterPick { get; set; }
        public Guid? MasterGameID { get; set; }
        public bool ManagerOverride { get; set; }
    }
}
