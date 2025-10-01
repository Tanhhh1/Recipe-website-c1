using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EProject.NETCore.Models;

public partial class User
{
    public int Id { get; set; }
    [Required(ErrorMessage = "Username is required.")]
    public string Username { get; set; } = null!;
    [Required(ErrorMessage = "Password is required.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long.")]
    public string Password { get; set; } = null!;
    [Required(ErrorMessage = "Email is required.")]
    public string Email { get; set; } = null!;
    [Required(ErrorMessage = "Fullname is required.")]
    public string Fullname { get; set; } = null!;

    public byte ?MembershipType { get; set; }

    public DateTime ?ExpirationDate { get; set; }

    public bool Role { get; set; }

    public virtual ICollection<Guidance> Guidances { get; set; } = new List<Guidance>();
}
