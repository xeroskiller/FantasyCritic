using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FantasyCritic.Lib.Domain;
using NodaTime;

namespace FantasyCritic.MySQL.Entities
{
    internal class MasterGameYearEntity
    {
        public Guid MasterGameID { get; set; }
        public string GameName { get; set; }
        public string EstimatedReleaseDate { get; set; }
        public DateTime? ReleaseDate { get; set; }
        public int? OpenCriticID { get; set; }
        public decimal? CriticScore { get; set; }
        public int MinimumReleaseYear { get; set; }
        public int EligibilityLevel { get; set; }
        public bool YearlyInstallment { get; set; }
        public bool EarlyAccess { get; set; }
        public string BoxartFileName { get; set; }
        public decimal PercentStandardGame { get; set; }
        public decimal PercentCounterPick { get; set; }
        public decimal AverageDraftPosition { get; set; }

        public MasterGameYear ToDomain(IEnumerable<MasterSubGame> subGames, EligibilityLevel eligibilityLevel, int year)
        {
            LocalDate? releaseDate = null;
            if (ReleaseDate.HasValue)
            {
                releaseDate = LocalDate.FromDateTime(ReleaseDate.Value);
            }

            var masterGame =  new MasterGame(MasterGameID, GameName, EstimatedReleaseDate, releaseDate, OpenCriticID, CriticScore, MinimumReleaseYear, eligibilityLevel, 
                YearlyInstallment, EarlyAccess, subGames.ToList(), BoxartFileName);

            return new MasterGameYear(masterGame, year, PercentStandardGame, PercentCounterPick, AverageDraftPosition);
        }
    }
}
