using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TAIBackend.Model;

namespace TAIBackend.routes.subscriptions;

[Route("subscriptions")]
public class SubscriptionController : Controller
{
    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    [HttpGet("me")]
    public async Task<IActionResult> GetSubscibedAccounts(YoutubeContext db)
    {
        try
        {
            var ownerAccountId = Int64.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var subs = await db.Subscriptions.Where(s => s.Owneraccountid == ownerAccountId).ToListAsync();
            return Ok(new
            {
                data = subs.ToArray().Select(s => new
                {
                    id = s.Id, subscribedId = s.Owneraccountid
                }),
                count = subs.Count
            });
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    [HttpPost("user/{userId}/subscribe")]
    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    [RequiresUserAccount]
    public async Task<IActionResult> CreateSubscription(YoutubeContext db, long userId)
    {
        try
        {
            await db.Accounts.SingleAsync(a => a.Id == userId);
            var sub = new Subscription
            {
                Owneraccountid = Int64.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value),
                Subscribedaccountid = userId
            };

            await db.Subscriptions.AddAsync(sub);
            await db.SaveChangesAsync();
            return StatusCode(201, new { accountId = sub.Subscribedaccountid });
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    [HttpDelete("user/{userId}/subscribe")]
    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    [RequiresUserAccount]
    public async Task<IActionResult> DeleteSubscription(YoutubeContext db, long userId)
    {
        try
        {
            await db.Accounts.SingleAsync(a => a.Id == userId);
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
            return StatusCode(200, new { accountId = sub.Subscribedaccountid });
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}