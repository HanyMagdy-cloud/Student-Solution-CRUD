using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace StudentApi.Models;

public class Student
{
    public int Id { get; set; }

    [Required]
    public required string Name { get; set; }

    [Required, EmailAddress]
    public required string Email { get; set; }

    public string? Phone { get; set; }

    [DisplayName("Date of Birth")]
    public DateTime? DateOfBearth { get; set; }  
}
