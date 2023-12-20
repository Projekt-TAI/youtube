using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
            var ownerAccountId = Int64.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var subs = await db.Subscriptions.Where(s => s.Owneraccountid == ownerAccountId).Include(s=>s.Subscribedaccount).ToListAsync();
            return Ok(
                subs.ToArray().Select(s => new
                {
                    userId = s.Owneraccountid, userFullName = s.Subscribedaccount.Fullname
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
            await db.Accounts.SingleAsync(a => a.Id == userId);
            var s = await db.Subscriptions.Where(s =>
                s.Owneraccountid == Int64.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value) &&
                s.Subscribedaccountid == userId).ToListAsync();
            
            if (s.Count > 0)
            {
                return BadRequest($"User has already subscribed to user {userId}");
            }
            var sub = new Subscription
            {
                Owneraccountid = Int64.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value),
                Subscribedaccountid = userId
            };

            await db.Subscriptions.AddAsync(sub);
            await db.SaveChangesAsync();
            return StatusCode(201, new { userId = sub.Subscribedaccountid, userFullName = sub.Subscribedaccount.Fullname });
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
            var sub = new Subscription
            {
                Owneraccountid = Int64.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value),
                Subscribedaccountid = userId
            };

            var s = await db.Subscriptions.SingleAsync(s =>
                s.Owneraccountid == Int64.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value) &&
                s.Subscribedaccountid == userId);
            db.Subscriptions.Attach(s);
            db.Subscriptions.Remove(s);
            await db.SaveChangesAsync();
            return StatusCode(200, new { userId = sub.Subscribedaccountid });
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}