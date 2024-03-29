﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TAIBackend.Model;

public partial class Comment
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    public long Videoid { get; set; }

    public string Data { get; set; } = null!;
    public bool IsEdited { get; set; } = false;

    public long Commenterid { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual Account Commenter { get; set; } = null!;

    public virtual Video Video { get; set; } = null!;
}
