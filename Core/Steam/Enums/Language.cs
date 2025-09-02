using System.ComponentModel.DataAnnotations;

namespace Core.Steam.Enums;

public enum Language
{
    [Display(Name= "Arabic")]
    Arabic,
    [Display(Name="Bulgarian")]
    Bulgarian,
    [Display(Name="Chinese (Simplified)")]
    SChinese,
    [Display(Name="Chinese (Traditional)")]
    TChinese,
    [Display(Name="Czech")]
    Czech,
    [Display(Name="Danish")]
    Danish,
    [Display(Name="Dutch")]
    Dutch,
    [Display(Name="English")]
    English,
    [Display(Name="Finnish")]
    Finnish,
    [Display(Name="French")]
    French,
    [Display(Name="German")]
    German,
    [Display(Name="Greek")]
    Greek,
    [Display(Name="Hungarian")]
    Hungarian,
    [Display(Name="Indonesian")]
    Indonesian,
    [Display(Name="Italian")]
    Italian,
    [Display(Name="Japanese")]
    Japanese,
    [Display(Name="Korean")]
    Koreana,
    [Display(Name="Norwegian")]
    Norwegian,
    [Display(Name="Polish")]
    Polish,
    [Display(Name="Portuguese")]
    Portuguese,
    [Display(Name="Portuguese-Brazil")]
    Brazilian,
    [Display(Name="Romanian")]
    Romanian,
    [Display(Name="Russian")]
    Russian,
    [Display(Name="Spanish-Spain")]
    Spanish,
    [Display(Name="Spanish-Latin America")]
    Latam,
    [Display(Name="Swedish")]
    Swedish,
    [Display(Name="Thai")]
    Thai,
    [Display(Name="Turkish")]
    Turkish,
    [Display(Name="Ukrainian")]
    Ukrainian,
    [Display(Name="Vietnamese")]
    Vietnamese
}