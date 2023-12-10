using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace TAIBackend.Model;

public partial class Video
{
    public DateTime CreatedAt {get; private set;} = DateTime.UtcNow;

    public int Id { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public long Owneraccountid { get; set; }

    public int Views { get; set; }

    public int Category { get; set; } = 0;

    public string ThumbnailSrc { get; set; } = "";

    public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();

    public virtual ICollection<Like> Likes { get; set; } = new List<Like>();

    public virtual Account Owneraccount { get; set; } = null!;
}
