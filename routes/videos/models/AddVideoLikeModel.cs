﻿namespace TAIBackend.routes.videos.models;

public partial class AddVideoLikeModel
{
    public long videoId { get; set; }

    public int value { get; set; }
}
