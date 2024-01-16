using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace TAIBackend.Model;

public partial class Video
{
    public DateTime CreatedAt {get; private set;} = DateTime.UtcNow;

    public long Id { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public long OwneraccountId { get; set; }

    public int Views { get; set; }

    public int Category { get; set; } = 0;

    public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();

    public virtual ICollection<Like> Likes { get; set; } = new List<Like>();

    public virtual Account Owneraccount { get; set; } = null!;
}
