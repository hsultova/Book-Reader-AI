using BookReaderApp.Models;
using BookReaderApp.Models.ViewModels;
using BookReaderApp.Repositories;

namespace BookReaderApp.Services;

public class FriendRequestService : IFriendRequestService
{
    private readonly IFriendRequestRepository _requests;
    private readonly ILogger<FriendRequestService> _logger;

    public FriendRequestService(
        IFriendRequestRepository requests,
        ILogger<FriendRequestService> logger)
    {
        _requests = requests;
        _logger = logger;
    }

    public async Task<FriendState> GetRelationshipAsync(string currentUserId, string otherUserId)
    {
        if (currentUserId == otherUserId)
        {
            return FriendState.None;
        }

        var existing = await _requests.GetBetweenAsync(currentUserId, otherUserId);
        return existing switch
        {
            null => FriendState.None,
            { Status: FriendRequestStatus.Accepted } => FriendState.Friends,
            { Status: FriendRequestStatus.Pending } r =>
                r.RequesterId == currentUserId ? FriendState.OutgoingPending : FriendState.IncomingPending,
            _ => FriendState.None // Rejected behaves like None: a fresh request may be sent.
        };
    }

    public async Task<int?> GetIncomingRequestIdAsync(string currentUserId, string otherUserId)
    {
        var existing = await _requests.GetBetweenAsync(currentUserId, otherUserId);
        return existing is { Status: FriendRequestStatus.Pending } && existing.AddresseeId == currentUserId
            ? existing.Id
            : null;
    }

    public async Task SendRequestAsync(string requesterId, string addresseeId)
    {
        if (requesterId == addresseeId)
        {
            return;
        }

        var existing = await _requests.GetBetweenAsync(requesterId, addresseeId);
        if (existing is null)
        {
            await _requests.AddAsync(new FriendRequest
            {
                RequesterId = requesterId,
                AddresseeId = addresseeId,
                Status = FriendRequestStatus.Pending,
                CreatedAt = DateTime.UtcNow
            });
            await _requests.SaveChangesAsync();
            _logger.LogInformation("Friend request sent from {Requester} to {Addressee}.", requesterId, addresseeId);
            return;
        }

        // A live relationship already exists; nothing to do.
        if (existing.Status is FriendRequestStatus.Pending or FriendRequestStatus.Accepted)
        {
            return;
        }

        // A previously rejected row: reuse it so the unique index isn't violated.
        existing.RequesterId = requesterId;
        existing.AddresseeId = addresseeId;
        existing.Status = FriendRequestStatus.Pending;
        existing.CreatedAt = DateTime.UtcNow;
        existing.RespondedAt = null;
        _requests.Update(existing);
        await _requests.SaveChangesAsync();
        _logger.LogInformation("Friend request re-sent from {Requester} to {Addressee}.", requesterId, addresseeId);
    }

    public async Task AcceptAsync(int requestId, string currentUserId) =>
        await RespondAsync(requestId, currentUserId, FriendRequestStatus.Accepted);

    public async Task RejectAsync(int requestId, string currentUserId) =>
        await RespondAsync(requestId, currentUserId, FriendRequestStatus.Rejected);

    private async Task RespondAsync(int requestId, string currentUserId, FriendRequestStatus status)
    {
        var request = await _requests.GetByIdAsync(requestId);

        // Only the addressee may respond, and only while the request is still pending.
        if (request is null || request.AddresseeId != currentUserId || request.Status != FriendRequestStatus.Pending)
        {
            return;
        }

        request.Status = status;
        request.RespondedAt = DateTime.UtcNow;
        _requests.Update(request);
        await _requests.SaveChangesAsync();
        _logger.LogInformation("Friend request {RequestId} {Status} by {User}.", requestId, status, currentUserId);
    }

    public async Task<FriendsViewModel> GetFriendsPageAsync(string currentUserId)
    {
        var accepted = await _requests.GetAcceptedForUserAsync(currentUserId);
        var incoming = await _requests.GetIncomingPendingAsync(currentUserId);
        var outgoing = await _requests.GetOutgoingPendingAsync(currentUserId);

        return new FriendsViewModel
        {
            // The friend is whichever side of the accepted row isn't the current user.
            Friends = accepted
                .Select(f => f.RequesterId == currentUserId
                    ? ToItem(f.Addressee, requestId: null)
                    : ToItem(f.Requester, requestId: null))
                .ToList(),
            IncomingRequests = incoming
                .Select(f => ToItem(f.Requester, f.Id))
                .ToList(),
            OutgoingRequests = outgoing
                .Select(f => ToItem(f.Addressee, f.Id))
                .ToList()
        };
    }

    private static FriendListItem ToItem(ApplicationUser? user, int? requestId) =>
        new(
            user?.Id ?? string.Empty,
            user?.DisplayName ?? "Unknown",
            user?.ProfilePicturePath,
            requestId);
}
