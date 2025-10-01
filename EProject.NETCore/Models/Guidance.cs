using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EProject.NETCore.Models;

public partial class Guidance
{
    public int Id { get; set; }
    [Required(ErrorMessage = "Image cannot be empty")]
    public string Img { get; set; } = null!;

    [Required(ErrorMessage = "Title cannot be empty")]
    public string Title { get; set; } = null!;
    [Required(ErrorMessage = "Content cannot be empty")]
    public string Content { get; set; } = null!;

    public bool Type { get; set; }

    public bool IsFree { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public int UserId { get; set; }

    public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();

    public virtual User User { get; set; } = null!;
}
