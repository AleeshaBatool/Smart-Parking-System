using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartParkingSystem.Models
{
    public class Parking
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id { get; set; }
        public string name { get; set; }
        public string Parkingtype { get; set; }
        public double priceperhour { get; set; }
        public bool isOccupied { get; set; }
        [ValidateNever]
        public virtual ICollection<CarParking> CarParkings { get; set; }
    }
}
