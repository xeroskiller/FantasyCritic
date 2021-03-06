using System;
using FantasyCritic.Lib.Domain.ScoringSystems;
using FantasyCritic.Lib.Enums;

namespace FantasyCritic.Lib.Domain.Requests
{
    public class EditLeagueYearParameters
    {
        public EditLeagueYearParameters(FantasyCriticUser manager, Guid leagueID, int year, int standardGames, int gamesToDraft, int counterPicks, 
            EligibilityLevel maximumEligibilityLevel, bool allowYearlyInstallments, 
            bool allowEarlyAccess, DraftSystem draftSystem, PickupSystem pickupSystem, ScoringSystem scoringSystem)
        {
            Manager = manager;
            LeagueID = leagueID;
            Year = year;
            StandardGames = standardGames;
            GamesToDraft = gamesToDraft;
            CounterPicks = counterPicks;
            AllowYearlyInstallments = allowYearlyInstallments;
            AllowEarlyAccess = allowEarlyAccess;
            MaximumEligibilityLevel = maximumEligibilityLevel;
            DraftSystem = draftSystem;
            PickupSystem = pickupSystem;
            ScoringSystem = scoringSystem;
        }

        public FantasyCriticUser Manager { get; }
        public Guid LeagueID { get; }
        public int Year { get; }
        public int StandardGames { get; }
        public int GamesToDraft { get; }
        public int CounterPicks { get; }
        public EligibilityLevel MaximumEligibilityLevel { get; }
        public bool AllowYearlyInstallments { get; set; }
        public bool AllowEarlyAccess { get; set; }
        public DraftSystem DraftSystem { get; }
        public PickupSystem PickupSystem { get; }
        public ScoringSystem ScoringSystem { get; }
    }
}
