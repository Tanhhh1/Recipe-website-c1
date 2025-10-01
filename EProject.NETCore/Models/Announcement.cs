using System;
using System.Collections.Generic;

namespace EProject.NETCore.Models;

public partial class Announcement
{
    public int Id { get; set; }

    public int CompetitionId { get; set; }

    public DateTime Date { get; set; }

    public virtual Competition Competition { get; set; } = null!;
}
