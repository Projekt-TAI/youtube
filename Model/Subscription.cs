using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TAIBackend.Model;

public class Subscription
{    
    public virtual Account Owneraccount { get; set; }
    
    public long OwneraccountId { get; set; }
    
    public virtual Account Subscribedaccount { get; set; }
    
    public long SubscribedaccountId { get; set; }
}