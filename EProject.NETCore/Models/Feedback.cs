using System;
using System.Collections.Generic;

namespace EProject.NETCore.Models;

public partial class Feedback
{
    public int Id { get; set; }

    public string Fullname { get; set; } = null!;

    public string Email { get; set; } = null!;

    public int GuidanceId { get; set; }

    public string Content { get; set; } = null!;

    public DateTime CreatedDate { get; set; }

    public virtual Guidance Guidance { get; set; } = null!;
}
