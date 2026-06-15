using BookReaderApp.Data;
using BookReaderApp.Models;
using Microsoft.EntityFrameworkCore;

namespace BookReaderApp.Repositories;

public class FriendRequestRepository : EfRepository<FriendRequest>, IFriendRequestRepository
{
    public FriendRequestRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<FriendRequest?> GetBetweenAsync(string userAId, string userBId) =>
        await Set.FirstOrDefaultAsync(f =>
            (f.RequesterId == userAId && f.AddresseeId == userBId) ||
            (f.RequesterId == userBId && f.AddresseeId == userAId));

    public async Task<IReadOnlyList<FriendRequest>> GetAcceptedForUserAsync(string userId) =>
        await Set
            .Include(f => f.Requester)
            .Include(f => f.Addressee)
            .Where(f => f.Status == FriendRequestStatus.Accepted &&
                        (f.RequesterId == userId || f.AddresseeId == userId))
            .OrderByDescending(f => f.RespondedAt)
            .ToListAsync();

    public async Task<IReadOnlyList<FriendRequest>> GetIncomingPendingAsync(string userId) =>
        await Set
            .Include(f => f.Requester)
            .Where(f => f.Status == FriendRequestStatus.Pending && f.AddresseeId == userId)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();

    public async Task<IReadOnlyList<FriendRequest>> GetOutgoingPendingAsync(string userId) =>
        await Set
            .Include(f => f.Addressee)
            .Where(f => f.Status == FriendRequestStatus.Pending && f.RequesterId == userId)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();
}
