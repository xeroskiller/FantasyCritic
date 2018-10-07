using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;

namespace FantasyCritic.Lib.Domain
{
    public class ClaimGameDomainRequest
    {
        public ClaimGameDomainRequest(Publisher publisher, string gameName, bool acquisition, bool counterPick, bool managerOverride, Maybe<MasterGame> masterGame)
        {
            Publisher = publisher;
            GameName = gameName;
            Acquisition = acquisition;
            CounterPick = counterPick;
            ManagerOverride = managerOverride;
            MasterGame = masterGame;
        }

        public Publisher Publisher { get; }
        public string GameName { get; }
        public bool Acquisition { get; }
        public bool CounterPick { get; }
        public bool ManagerOverride { get; }
        public Maybe<MasterGame> MasterGame { get; }
    }
}
