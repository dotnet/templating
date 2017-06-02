using System.ComponentModel.DataAnnotations;

namespace Company.WebApplication1.Identity.Models.ApplicationViewModels
{
    public class CreateApplicationViewModel
    {
        [Required]
        public string Name { get; set; }
    }
}
