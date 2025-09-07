using Core.Models;
using Core.Steam.Enums;

namespace Core.Steam.Models;

public class SteamReviewQueryParameters : ReviewQueryParameters
{
    public int NumPerPage { get; set; } = 100;
    public Filter Filter { get; set; } = Filter.Recent;
    public Language[]? Languages { get; set; }
    public ReviewType ReviewType { get; set; } = ReviewType.All;
    public PurchaseType PurchaseType { get; set; } = PurchaseType.All;
    public bool FilterOffTopicActivity { get; set; } = true;
    public string Cursor { get; set; } = "*";
}
