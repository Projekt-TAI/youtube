using System;
using System.Collections.Generic;

namespace TAIBackend.Model;

public partial class Like
{
    public long VideoId { get; set; }

    public long AccountId { get; set; }

    public bool Unlike { get; set; }

    public virtual Account Account { get; set; } = null!;

    public virtual Video Video { get; set; } = null!;
}
