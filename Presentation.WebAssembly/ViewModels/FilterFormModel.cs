using System.ComponentModel.DataAnnotations;
using Core.Steam.Enums;

namespace Presentation.WebAssembly.ViewModels;

public class FilterFormModel
{
    [Required(ErrorMessage = "Filter type is required")]
    public Filter FilterType { get; set; } = Filter.Recent;

    [Required(ErrorMessage = "Review type is required")]
    public ReviewType ReviewType { get; set; } = ReviewType.All;

    public string[] SelectedLanguages { get; set; } = ["All"];

    [Range(1, 100, ErrorMessage = "Number of reviews per page must be between 1 and 100")]
    public int NumPerPage { get; set; } = 100;

    [Required(ErrorMessage = "Purchase type is required")]
    public PurchaseType PurchaseType { get; set; } = PurchaseType.All;

    public bool FilterOffTopicActivity { get; set; } = true;
}
