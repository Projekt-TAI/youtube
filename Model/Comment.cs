using System;
using System.Collections.Generic;

namespace TAIBackend.Model;

public partial class Comment
{
    public int Id { get; set; }

    public int Videoid { get; set; }

    public string Data { get; set; } = null!;

    public long Commenterid { get; set; }

    public virtual Account Commenter { get; set; } = null!;

    public virtual Video Video { get; set; } = null!;
}
