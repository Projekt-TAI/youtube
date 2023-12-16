using System;
using System.Collections.Generic;

namespace TAIBackend.Model;

public partial class Account
{
    public long Id { get; set; }

    public string Firstname { get; set; } = null!;

    public string Fullname { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string ProfilePicUrl { get; set; } = null!;

    public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();

    public virtual ICollection<Like> Likes { get; set; } = new List<Like>();

    public virtual ICollection<Video> Videos { get; set; } = new List<Video>();
}
