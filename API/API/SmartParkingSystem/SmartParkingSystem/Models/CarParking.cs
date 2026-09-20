using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using SmartParkingSystem.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class CarParking
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int id { get; set; }

    [ForeignKey("Parking")]
    public int parkingID { get; set; }

    public string carNumber { get; set; }
    public DateTime? parkingStartTime { get; set; }
    [ValidateNever]
    public DateTime? parkingEndTime { get; set; }
    public double totalAmount { get; set; }
    [ValidateNever]
    public virtual Parking Parking { get; set; }

    [StringLength(15)]
    public string EasyPaisaPhoneNumber { get; set; }

    public bool IsParked { get; set; } =false;

    [Required]
    [EmailAddress]
    [Display(Name = "Email Address")]
    public string Email { get; set; }

}
