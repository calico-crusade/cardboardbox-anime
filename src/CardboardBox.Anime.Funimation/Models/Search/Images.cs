using System.Text.Json.Serialization;

namespace CardboardBox.Anime.Funimation;

public class Images
{
    [JsonPropertyName("appleHorizontalBannerMovie")]
    public string? AppleHorizontalBannerMovie { get; set; }

    [JsonPropertyName("appleHorizontalBannerShow")]
    public string? AppleHorizontalBannerShow { get; set; }

    [JsonPropertyName("applePosterCover")]
    public string? ApplePosterCover { get; set; }

    [JsonPropertyName("appleSquareCover")]
    public string? AppleSquareCover { get; set; }

    [JsonPropertyName("backgroundImageAppletvfiretv")]
    public string? BackgroundImageAppletvfiretv { get; set; }

    [JsonPropertyName("backgroundImageXbox_360")]
    public string? BackgroundImageXbox360 { get; set; }

    [JsonPropertyName("continueWatchingDesktop")]
    public string? ContinueWatchingDesktop { get; set; }

    [JsonPropertyName("continueWatchingMobile")]
    public string? ContinueWatchingMobile { get; set; }

    [JsonPropertyName("featuredSpotlightShowPhone")]
    public string? FeaturedSpotlightShowPhone { get; set; }

    [JsonPropertyName("featuredSpotlightShowTablet")]
    public string? FeaturedSpotlightShowTablet { get; set; }

    [JsonPropertyName("newShowDetailHero")]
    public string? NewShowDetailHero { get; set; }

    [JsonPropertyName("newShowDetailHeroPhone")]
    public string? NewShowDetailHeroPhone { get; set; }

    [JsonPropertyName("showBackgroundSite")]
    public string? ShowBackgroundSite { get; set; }

    [JsonPropertyName("showDetailBoxArtPhone")]
    public string? ShowDetailBoxArtPhone { get; set; }

    [JsonPropertyName("showDetailBoxArtTablet")]
    public string? ShowDetailBoxArtTablet { get; set; }

    [JsonPropertyName("showDetailBoxArtXbox_360")]
    public string? ShowDetailBoxArtXbox360 { get; set; }

    [JsonPropertyName("showDetailHeaderDesktop")]
    public string? ShowDetailHeaderDesktop { get; set; }

    [JsonPropertyName("showDetailHeaderMobile")]
    public string? ShowDetailHeaderMobile { get; set; }

    [JsonPropertyName("showDetailHeroDesktop")]
    public string? ShowDetailHeroDesktop { get; set; }

    [JsonPropertyName("showDetailHeroSite")]
    public string? ShowDetailHeroSite { get; set; }

    [JsonPropertyName("showKeyart")]
    public string? ShowKeyart { get; set; }

    [JsonPropertyName("showLogo")]
    public string? ShowLogo { get; set; }

    [JsonPropertyName("showMasterKeyArt")]
    public string? ShowMasterKeyArt { get; set; }

    [JsonPropertyName("showThumbnail")]
    public string? ShowThumbnail { get; set; }
}
