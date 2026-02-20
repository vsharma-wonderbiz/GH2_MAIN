using System.ComponentModel.DataAnnotations;

namespace Application.DTOS
{
    public class CreateAssetDto
    {
        [Required(ErrorMessage = "Asset name is required")]
        [StringLength(100, ErrorMessage = "Asset name cannot exceed 100 characters")]
        public string Name { get; set; }

     
        [Range(1, int.MaxValue, ErrorMessage = "ParentAssetId must be a positive number")]
        public int? ParentAssetId { get; set; }
    }
}