using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TAIBackend.Model;

public class Subscription
{    
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    public virtual Account Owneraccount { get; set; }
    
    public long Owneraccountid { get; set; }
    
    public virtual Account Subscribedaccount { get; set; }
    
    public long Subscribedaccountid { get; set; }
}