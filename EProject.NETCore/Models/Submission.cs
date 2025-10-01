using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EProject.NETCore.Models;

public partial class Submission
{
    public int Id { get; set; }

    public int CompetitionId { get; set; }
    [Required(ErrorMessage = "Fullname is required.")]
    public string Fullname { get; set; } = null!;
    [Required(ErrorMessage = "Email is required.")]
    public string Email { get; set; } = null!;
    [Required(ErrorMessage = "Title is required.")]
    public string Title { get; set; } = null!;
    [Required(ErrorMessage = "Content is required.")]
    public string Content { get; set; } = null!;

    public DateTime CreatedDate { get; set; }

    public bool? IsWinner { get; set; }

    public virtual Competition Competition { get; set; } = null!;
}
