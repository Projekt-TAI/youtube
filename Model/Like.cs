using System;
using System.Collections.Generic;

namespace TAIBackend.Model;

public partial class Like
{
    public int Id { get; set; }

    public int Video { get; set; }

    public long Account { get; set; }

    public bool Unlike { get; set; }

    public virtual Account AccountNavigation { get; set; } = null!;

    public virtual Video VideoNavigation { get; set; } = null!;
}
