using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using TAIBackend.Model;

namespace TAIBackend.routes.subscriptions;

[Route("subscriptions")]
[Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
public class SubscriptionController : Controller
{
    [HttpGet("me")]
    public async Task<IActionResult> GetSubscibedAccounts(YoutubeContext db)
    {
        try
        {
            var ownerAccountId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var subs = await db.Subscriptions
                .Include(s => s.Subscribedaccount)
                .Where(s => s.OwneraccountId == ownerAccountId)
                .ToListAsync();

            return Ok(
                subs.ToArray().Select(s => new
                {
                    userId = s.SubscribedaccountId, 
                    userFullName = s.Subscribedaccount.Fullname,
                    profilePictureSrc = s.Subscribedaccount.ProfilePicUrl,
                })
            );
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    [HttpPost("user/{userId}/subscribe")]
    [RequiresUserAccount]
    public async Task<IActionResult> CreateSubscription(YoutubeContext db, long userId)
    {
        try
        {
            var ownerAccountId = User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
            var ownerAccountIdParsed = long.Parse(ownerAccountId);

            // Make sure that the account we are trying to subscribe exists
            await db.Accounts.SingleAsync(a => a.Id == userId);

            var sub = new Subscription
            {
                OwneraccountId = ownerAccountIdParsed,
                SubscribedaccountId = userId
            };

            await db.Subscriptions.AddAsync(sub);
            await db.SaveChangesAsync();

            return StatusCode(201, new 
            { 
                userId = sub.SubscribedaccountId, 
                userFullName = sub.Subscribedaccount.Fullname,
                profilePictureSrc = sub.Subscribedaccount.ProfilePicUrl,
            });
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    [HttpDelete("user/{userId}/subscribe")]
    [RequiresUserAccount]
    public async Task<IActionResult> DeleteSubscription(YoutubeContext db, long userId)
    {
        try
        {
            var ownerAccountId = User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
            var ownerAccountIdParsed = long.Parse(ownerAccountId);

            var s = await db.Subscriptions
                .SingleAsync(s =>
                    s.OwneraccountId == ownerAccountIdParsed &&
                    s.SubscribedaccountId == userId
                );

            db.Subscriptions.Remove(s);

            await db.SaveChangesAsync();

            return StatusCode(200);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}