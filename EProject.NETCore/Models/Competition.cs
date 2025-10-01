using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EProject.NETCore.Models;

public partial class Competition
{
    public int Id { get; set; }
    [Required(ErrorMessage = "Title is required.")]
    public string Title { get; set; } = null!;
    [Required(ErrorMessage = "Description is required.")]
    public string Description { get; set; } = null!;
    [Required(ErrorMessage = "StartDate is required.")]
    public DateTime StartDate { get; set; }
    [Required(ErrorMessage = "EndDate is required.")]
    public DateTime EndDate { get; set; }

    public virtual ICollection<Announcement> Announcements { get; set; } = new List<Announcement>();

    public virtual ICollection<Submission> Submissions { get; set; } = new List<Submission>();
}
