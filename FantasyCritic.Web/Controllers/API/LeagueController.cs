using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using FantasyCritic.Lib.Domain;
using FantasyCritic.Lib.Domain.Requests;
using FantasyCritic.Lib.Domain.Results;
using FantasyCritic.Lib.Domain.ScoringSystems;
using FantasyCritic.Lib.Enums;
using FantasyCritic.Lib.Services;
using FantasyCritic.Web.Hubs;
using FantasyCritic.Web.Models;
using FantasyCritic.Web.Models.Requests;
using FantasyCritic.Web.Models.Responses;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using NodaTime;

namespace FantasyCritic.Web.Controllers.API
{
    [Route("api/[controller]/[action]")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class LeagueController : Controller
    {
        private readonly FantasyCriticUserManager _userManager;
        private readonly FantasyCriticService _fantasyCriticService;
        private readonly IClock _clock;
        private readonly IHubContext<UpdateHub> _hubcontext;

        public LeagueController(FantasyCriticUserManager userManager, FantasyCriticService fantasyCriticService, IClock clock, IHubContext<UpdateHub> hubcontext)
        {
            _userManager = userManager;
            _fantasyCriticService = fantasyCriticService;
            _clock = clock;
            _hubcontext = hubcontext;
        }

        public async Task<IActionResult> LeagueOptions()
        {
            var supportedYears = await _fantasyCriticService.GetSupportedYears();
            var openYears = supportedYears.Where(x => x.OpenForCreation).Select(x => x.Year);
            IReadOnlyList<EligibilityLevel> eligibilityLevels = await _fantasyCriticService.GetEligibilityLevels();
            LeagueOptionsViewModel viewModel = new LeagueOptionsViewModel(openYears, DraftSystem.GetAllPossibleValues(),
                PickupSystem.GetAllPossibleValues(), ScoringSystem.GetAllPossibleValues(), eligibilityLevels);

            return Ok(viewModel);
        }

        public async Task<IActionResult> MyLeagues()
        {
            var currentUser = await _userManager.FindByNameAsync(User.Identity.Name);
            IReadOnlyList<League> myLeagues = await _fantasyCriticService.GetLeaguesForUser(currentUser);

            List<LeagueViewModel> viewModels = new List<LeagueViewModel>();
            foreach (var league in myLeagues)
            {
                bool isManager = (league.LeagueManager.UserID == currentUser.UserID);
                viewModels.Add(new LeagueViewModel(league, isManager));
            }

            return Ok(viewModels);
        }

        public async Task<IActionResult> MyInvites()
        {
            var currentUser = await _userManager.FindByNameAsync(User.Identity.Name);
            var invitedLeagues = await _fantasyCriticService.GetLeaguesInvitedTo(currentUser);
            var viewModels = invitedLeagues.Select(x => new InviteViewModel(x));
            return Ok(viewModels);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetLeague(Guid id)
        {
            Maybe<League> league = await _fantasyCriticService.GetLeagueByID(id);
            if (league.HasNoValue)
            {
                return NotFound();
            }

            var currentUser = await _userManager.FindByNameAsync(User.Identity.Name);
            var playersInLeague = await _fantasyCriticService.GetUsersInLeague(league.Value);
            bool userIsInLeague = playersInLeague.Any(x => x.UserID == currentUser.UserID);

            var inviteesToLeague = await _fantasyCriticService.GetOutstandingInvitees(league.Value);
            bool userIsInvitedToLeague = inviteesToLeague.Any(x => x == currentUser.EmailAddress);
            if (!userIsInLeague && !userIsInvitedToLeague)
            {
                return Unauthorized();
            }

            bool neverStarted = true;
            foreach (var year in league.Value.Years)
            {
                var leagueYear = await _fantasyCriticService.GetLeagueYear(league.Value.LeagueID, year);
                if (leagueYear.Value.PlayStatus.PlayStarted)
                {
                    neverStarted = false;
                }
            }

            bool isManager = (league.Value.LeagueManager.UserID == currentUser.UserID);
            var leagueViewModel = new LeagueViewModel(league.Value, isManager, playersInLeague, inviteesToLeague,
                userIsInvitedToLeague, neverStarted);
            return Ok(leagueViewModel);
        }

        public async Task<IActionResult> GetLeagueYear(Guid leagueID, int year)
        {
            Maybe<LeagueYear> leagueYear = await _fantasyCriticService.GetLeagueYear(leagueID, year);
            if (leagueYear.HasNoValue)
            {
                throw new Exception("Something went really wrong, no options are set up for this league.");
            }

            var currentUser = await _userManager.FindByNameAsync(User.Identity.Name);
            var usersInLeague = await _fantasyCriticService.GetUsersInLeague(leagueYear.Value.League);
            bool userIsInLeague = usersInLeague.Any(x => x.UserID == currentUser.UserID);

            var inviteesToLeague = await _fantasyCriticService.GetOutstandingInvitees(leagueYear.Value.League);
            bool userIsInvitedToLeague = inviteesToLeague.Any(x => x == currentUser.EmailAddress);
            if (!userIsInLeague && !userIsInvitedToLeague)
            {
                return Unauthorized();
            }

            var publishersInLeague = await _fantasyCriticService.GetPublishersInLeagueForYear(leagueYear.Value.League, leagueYear.Value.Year);
            var supportedYear = (await _fantasyCriticService.GetSupportedYears()).SingleOrDefault(x => x.Year == year);
            if (supportedYear is null)
            {
                return BadRequest();
            }

            StartDraftResult startDraftResult = await _fantasyCriticService.GetStartDraftResult(leagueYear.Value, publishersInLeague, usersInLeague);
            Maybe<Publisher> nextDraftPublisher = await _fantasyCriticService.GetNextDraftPublisher(leagueYear.Value);
            DraftPhase draftPhase = await _fantasyCriticService.GetDraftPhase(leagueYear.Value);

            var userPublisher = publishersInLeague.SingleOrDefault(x => x.User.UserID == currentUser.UserID);
            IReadOnlyList<PublisherGame> availableCounterPicks = new List<PublisherGame>();
            if (nextDraftPublisher.HasValue)
            {
                availableCounterPicks = await _fantasyCriticService.GetAvailableCounterPicks(leagueYear.Value, nextDraftPublisher.Value);
            }

            LeagueWideValues leagueWideValues = await _fantasyCriticService.GetLeagueWideValues();
                
            var leagueViewModel = new LeagueYearViewModel(leagueYear.Value, supportedYear, publishersInLeague, currentUser, userPublisher, _clock,
                leagueYear.Value.PlayStatus, startDraftResult, usersInLeague, nextDraftPublisher, draftPhase, availableCounterPicks, leagueWideValues);
            return Ok(leagueViewModel);
        }

        public async Task<IActionResult> GetLeagueActions(Guid leagueID, int year)
        {
            Maybe<LeagueYear> leagueYear = await _fantasyCriticService.GetLeagueYear(leagueID, year);
            if (leagueYear.HasNoValue)
            {
                throw new Exception("Something went really wrong, no options are set up for this league.");
            }

            var currentUser = await _userManager.FindByNameAsync(User.Identity.Name);
            var usersInLeague = await _fantasyCriticService.GetUsersInLeague(leagueYear.Value.League);
            bool userIsInLeague = usersInLeague.Any(x => x.UserID == currentUser.UserID);

            var inviteesToLeague = await _fantasyCriticService.GetOutstandingInvitees(leagueYear.Value.League);
            bool userIsInvitedToLeague = inviteesToLeague.Any(x => x == currentUser.EmailAddress);
            if (!userIsInLeague && !userIsInvitedToLeague)
            {
                return Unauthorized();
            }

            var leagueActions = await _fantasyCriticService.GetLeagueActions(leagueYear.Value);

            var viewModels = leagueActions.Select(x => new LeagueActionViewModel(x, _clock));
            viewModels = viewModels.OrderByDescending(x => x.Timestamp);
            return Ok(viewModels);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPublisher(Guid id)
        {
            Maybe<Publisher> publisher = await _fantasyCriticService.GetPublisher(id);
            if (publisher.HasNoValue)
            {
                return NotFound();
            }

            var currentUser = await _userManager.FindByNameAsync(User.Identity.Name);
            var playersInLeague = await _fantasyCriticService.GetUsersInLeague(publisher.Value.League);
            bool userIsInLeague = playersInLeague.Any(x => x.UserID == currentUser.UserID);
            if (!userIsInLeague)
            {
                return Unauthorized();
            }

            bool leaguePlayingYear = publisher.Value.League.Years.Contains(publisher.Value.Year);
            if (!leaguePlayingYear)
            {
                return BadRequest("League is not playing that year.");
            }

            var requstedPlayerIsInLeague = playersInLeague.Any(x => x.UserID == publisher.Value.User.UserID);
            if (!requstedPlayerIsInLeague)
            {
                return BadRequest("Requested player is not in requested league.");
            }

            var publisherViewModel = new PublisherViewModel(publisher.Value, _clock);
            return Ok(publisherViewModel);
        }

        public async Task<IActionResult> GetLeagueYearOptions(Guid leagueID, int year)
        {
            Maybe<League> league = await _fantasyCriticService.GetLeagueByID(leagueID);
            if (league.HasNoValue)
            {
                return NotFound();
            }

            Maybe<LeagueYear> leagueYear = await _fantasyCriticService.GetLeagueYear(leagueID, year);
            if (leagueYear.HasNoValue)
            {
                throw new Exception("Something went really wrong, no options are set up for this league.");
            }

            var currentUser = await _userManager.FindByNameAsync(User.Identity.Name);
            var usersInLeague = await _fantasyCriticService.GetUsersInLeague(leagueYear.Value.League);
            bool userIsInLeague = usersInLeague.Any(x => x.UserID == currentUser.UserID);
            if (!userIsInLeague)
            {
                return Unauthorized();
            }

            var leagueViewModel = new LeagueYearSettingsViewModel(league.Value, leagueYear.Value);
            return Ok(leagueViewModel);
        }

        [HttpPost]
        public async Task<IActionResult> AcceptInvite([FromBody] AcceptInviteRequest request)
        {
            var currentUser = await _userManager.FindByNameAsync(User.Identity.Name);

            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var league = await _fantasyCriticService.GetLeagueByID(request.LeagueID);
            if (league.HasNoValue)
            {
                return BadRequest();
            }

            if (!currentUser.EmailConfirmed)
            {
                return BadRequest();
            }

            Result result = await _fantasyCriticService.AcceptInvite(league.Value, currentUser);
            if (result.IsFailure)
            {
                return BadRequest(result.Error);
            }

            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> CreatePublisher([FromBody] CreatePublisherRequest request)
        {
            var league = await _fantasyCriticService.GetLeagueByID(request.LeagueID);
            if (league.HasNoValue)
            {
                return BadRequest();
            }

            var currentUser = await _userManager.FindByNameAsync(User.Identity.Name);
            bool userIsInLeague = await _fantasyCriticService.UserIsInLeague(league.Value, currentUser);
            if (!userIsInLeague)
            {
                return Forbid();
            }

            var currentPublishers = await _fantasyCriticService.GetPublishersInLeagueForYear(league.Value, request.Year);
            var publisherForUser = currentPublishers.SingleOrDefault(x => x.User.UserID == currentUser.UserID);
            if (publisherForUser != null)
            {
                return BadRequest("You have already created a publisher for this this league/year.");
            }

            await _fantasyCriticService.CreatePublisher(league.Value, request.Year, currentUser, request.PublisherName, currentPublishers);
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> ChangePublisherName([FromBody] ChangePublisherNameRequest request)
        {
            var publisher = await _fantasyCriticService.GetPublisher(request.PublisherID);
            if (publisher.HasNoValue)
            {
                return BadRequest();
            }

            var currentUser = await _userManager.FindByNameAsync(User.Identity.Name);
            bool userIsInLeague = await _fantasyCriticService.UserIsInLeague(publisher.Value.League, currentUser);
            if (!userIsInLeague)
            {
                return Forbid();
            }

            if (publisher.Value.User.UserID != currentUser.UserID)
            {
                return Forbid();
            }

            await _fantasyCriticService.ChangePublisherName(publisher.Value, request.PublisherName);
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> DeclineInvite([FromBody] DeclineInviteRequest request)
        {
            var currentUser = await _userManager.FindByNameAsync(User.Identity.Name);

            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var league = await _fantasyCriticService.GetLeagueByID(request.LeagueID);
            if (league.HasNoValue)
            {
                return BadRequest();
            }

            Result result = await _fantasyCriticService.DeclineInvite(league.Value, currentUser);
            if (result.IsFailure)
            {
                return BadRequest(result.Error);
            }

            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> MakePickupBid([FromBody] PickupBidRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var publisher = await _fantasyCriticService.GetPublisher(request.PublisherID);
            if (publisher.HasNoValue)
            {
                return BadRequest();
            }

            Maybe<LeagueYear> leagueYear = await _fantasyCriticService.GetLeagueYear(publisher.Value.League.LeagueID, publisher.Value.Year);
            if (leagueYear.HasNoValue)
            {
                return BadRequest();
            }
            if (!leagueYear.Value.PlayStatus.PlayStarted)
            {
                return BadRequest("Play has not started for that year.");
            }

            var currentUser = await _userManager.FindByNameAsync(User.Identity.Name);
            bool userIsInLeague = await _fantasyCriticService.UserIsInLeague(publisher.Value.League, currentUser);
            bool userIsPublisher = (currentUser.UserID == publisher.Value.User.UserID);
            if (!userIsInLeague || !userIsPublisher)
            {
                return Forbid();
            }

            var masterGame = await _fantasyCriticService.GetMasterGame(request.MasterGameID);
            if (masterGame.HasNoValue)
            {
                return BadRequest("That master game does not exist.");
            }
            
            ClaimResult bidResult = await _fantasyCriticService.MakePickupBid(publisher.Value, masterGame.Value, request.BidAmount);
            var viewModel = new PickupBidResultViewModel(bidResult);

            return Ok(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> DeletePickupBid([FromBody] PickupBidDeleteRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var maybeBid = await _fantasyCriticService.GetPickupBid(request.BidID);
            if (maybeBid.HasNoValue)
            {
                return BadRequest("That bid does not exist.");
            }

            var publisher = maybeBid.Value.Publisher;
            var currentUser = await _userManager.FindByNameAsync(User.Identity.Name);
            bool userIsInLeague = await _fantasyCriticService.UserIsInLeague(publisher.League, currentUser);
            bool userIsPublisher = (currentUser.UserID == publisher.User.UserID);
            if (!userIsInLeague || !userIsPublisher)
            {
                return Forbid();
            }

            PickupBid bid = maybeBid.Value;
            Result result = await _fantasyCriticService.RemovePickupBid(bid);
            if (result.IsFailure)
            {
                return BadRequest(result.Error);
            }

            return Ok();
        }

        [HttpGet("{publisherID}")]
        public async Task<IActionResult> CurrentBids(Guid publisherID)
        {
            Maybe<Publisher> publisher = await _fantasyCriticService.GetPublisher(publisherID);
            if (publisher.HasNoValue)
            {
                return NotFound();
            }

            var currentUser = await _userManager.FindByNameAsync(User.Identity.Name);
            if (currentUser.UserID != publisher.Value.User.UserID)
            {
                return Forbid();
            }

            var bids = await _fantasyCriticService.GetActiveAcquistitionBids(publisher.Value);

            var viewModels = bids.Select(x => new PickupBidViewModel(x)).OrderBy(x => x.Priority);
            return Ok(viewModels);
        }

        [HttpPost]
        public async Task<IActionResult> DraftGame([FromBody] DraftGameRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var publisher = await _fantasyCriticService.GetPublisher(request.PublisherID);
            if (publisher.HasNoValue)
            {
                return BadRequest();
            }

            var currentUser = await _userManager.FindByNameAsync(User.Identity.Name);
            if (currentUser.UserID != publisher.Value.User.UserID)
            {
                return Forbid();
            }

            var league = await _fantasyCriticService.GetLeagueByID(publisher.Value.League.LeagueID);
            if (league.HasNoValue)
            {
                return BadRequest();
            }

            var leagueYear = await _fantasyCriticService.GetLeagueYear(league.Value.LeagueID, publisher.Value.Year);
            if (leagueYear.HasNoValue)
            {
                return BadRequest();
            }

            if (!leagueYear.Value.PlayStatus.DraftIsActive)
            {
                return BadRequest("You can't draft a game if the draft isn't active.");
            }

            var nextPublisher = await _fantasyCriticService.GetNextDraftPublisher(leagueYear.Value);
            if (nextPublisher.HasNoValue)
            {
                return BadRequest("There are no spots open to draft.");
            }

            if (!nextPublisher.Value.Equals(publisher.Value))
            {
                return BadRequest("That publisher is not next up for drafting.");
            }

            Maybe<MasterGame> masterGame = Maybe<MasterGame>.None;
            if (request.MasterGameID.HasValue)
            {
                masterGame = await _fantasyCriticService.GetMasterGame(request.MasterGameID.Value);
            }

            int? publisherPosition = null;
            int? overallPosition = null;
            var draftPhase = await _fantasyCriticService.GetDraftPhase(leagueYear.Value);
            if (draftPhase.Equals(DraftPhase.StandardGames))
            {
                publisherPosition = publisher.Value.PublisherGames.Count(x => !x.CounterPick) + 1;
                var publishers = await _fantasyCriticService.GetPublishersInLeagueForYear(league.Value, leagueYear.Value.Year);
                overallPosition = publishers.SelectMany(x => x.PublisherGames).Count(x => !x.CounterPick) + 1;

                if (request.CounterPick)
                {
                    return BadRequest("Not drafting counterPicks now.");
                }
            }

            if (draftPhase.Equals(DraftPhase.CounterPicks))
            {
                if (!request.CounterPick)
                {
                    return BadRequest("Not drafting standard games now.");
                }
            }

            ClaimGameDomainRequest domainRequest = new ClaimGameDomainRequest(publisher.Value, request.GameName, request.CounterPick, false, masterGame, publisherPosition, overallPosition);

            ClaimResult result = await _fantasyCriticService.ClaimGame(domainRequest);
            bool draftCompleted = await _fantasyCriticService.CompleteDraft(leagueYear.Value);
            var viewModel = new PlayerClaimResultViewModel(result);
            await _hubcontext.Clients.All.SendAsync("RefreshLeagueYear", leagueYear.Value);

            if (draftCompleted)
            {
                await _hubcontext.Clients.All.SendAsync("DraftFinished", leagueYear.Value);
            }

            return Ok(viewModel);
        }
    }
}
