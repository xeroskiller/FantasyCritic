﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FantasyCritic.Lib.Domain.Requests;
using NodaTime;

namespace FantasyCritic.Lib.Domain
{
    public class LeagueAction
    {
        public LeagueAction(Publisher publisher, Instant timestamp, string actionType, string description, bool managerAction)
        {
            Publisher = publisher;
            Timestamp = timestamp;
            ActionType = actionType;
            Description = description;
            ManagerAction = managerAction;
        }

        public LeagueAction(ClaimGameDomainRequest action, Instant timestamp)
        {
            Timestamp = timestamp;
            Publisher = action.Publisher;
            ActionType = "Publisher Game Claimed";
            Description = $"Claimed game: '{action.GameName}'";
            ManagerAction = true;
        }

        public LeagueAction(AssociateGameDomainRequest action, Instant timestamp)
        {
            Timestamp = timestamp;
            Publisher = action.Publisher;
            ActionType = "Publisher Game Associated";
            Description = $"Associated publisher game '{action.PublisherGame.GameName}' with master game '{action.MasterGame.GameName}'";
            ManagerAction = true;
        }

        public LeagueAction(RemoveGameDomainRequest action, Instant timestamp)
        {
            Timestamp = timestamp;
            Publisher = action.Publisher;
            ActionType = "Publisher Game Removed";
            Description = $"Removed game: '{action.PublisherGame.GameName}'";
            ManagerAction = true;
        }

        public LeagueAction(PickupBid action, Instant timestamp)
        {
            Timestamp = timestamp;
            Publisher = action.Publisher;
            ActionType = "Pickup Successful";
            Description = $"Acquired game '{action.MasterGame.GameName}' with a bid of ${action.BidAmount}";
            ManagerAction = false;
        }

        public LeagueAction(FailedPickupBid action, Instant timestamp)
        {
            Timestamp = timestamp;
            Publisher = action.PickupBid.Publisher;
            ActionType = "Pickup Failed";
            Description = $"Tried to acquire game '{action.PickupBid.MasterGame.GameName}' with a bid of ${action.PickupBid.BidAmount}. Failure reason: {action.FailureReason}";
            ManagerAction = false;
        }

        public Publisher Publisher { get; }
        public Instant Timestamp { get; }
        public string ActionType { get; }
        public string Description { get; }
        public bool ManagerAction { get; }
    }
}
