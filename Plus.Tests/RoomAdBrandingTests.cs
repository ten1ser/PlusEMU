using Plus.HabboHotel.Items;
using Xunit;

namespace Plus.Tests;

public class RoomAdBrandingTests
{
    [Theory]
    [InlineData("/nitro-assets/roomads/bank.png")]
    [InlineData("/nitro-assets/roomads/City_Hall-2.webp")]
    [InlineData("/nitro-assets/roomads/x.gif")]
    [InlineData("/nitro-assets/roomads/promo.jpeg")]
    [InlineData("")]
    public void BrandingAcceptsAnImageFromTheRoomAdsFolder(string image)
    {
        Assert.False(FurniExtraData.RejectsBranding(new[] { "imageUrl", image, "offsetX", "0", "offsetY", "-12", "offsetZ", "0", "scale", "100", "alpha", "100" }));
    }

    [Theory]
    [InlineData("https://example.invalid/a.png")]
    [InlineData("//example.invalid/a.png")]
    [InlineData("/nitro-assets/roomads/../camera/a.png")]
    [InlineData("/nitro-assets/roomads/sub/a.png")]
    [InlineData("/nitro-assets/roomads/a.svg")]
    [InlineData("/nitro-assets/roomads/a.png?x=1")]
    [InlineData("/nitro-assets/roomads/.png")]
    [InlineData("/nitro-assets/camera/a.png")]
    [InlineData("nitro-assets/roomads/a.png")]
    [InlineData("/NITRO-ASSETS/roomads/a.png")]
    [InlineData("/nitro-assets/roomads/a.png\n")]
    [InlineData("data:image/png;base64,AAAA")]
    public void BrandingRefusesEveryOtherImageAddress(string image)
    {
        Assert.True(FurniExtraData.RejectsBranding(new[] { "imageUrl", image, "offsetX", "0" }));
    }

    [Fact]
    public void BrandingAcceptsTheCompleteNativeRoomAdFormPayload()
    {
        Assert.False(FurniExtraData.RejectsBranding(new[] {
            "imageUrl", "/nitro-assets/roomads/coffeehouse.png",
            "clickUrl", "",
            "offsetX", "0",
            "offsetY", "0",
            "offsetZ", "0",
            "scale", "100",
            "alpha", "100"
        }));
    }

    [Theory]
    [InlineData("clickUrl", "")]
    [InlineData("ClickUrl", "")]
    [InlineData("CLICKURL", "")]
    public void BrandingRefusesASecondClickUrlPair(string key, string value)
    {
        Assert.True(FurniExtraData.RejectsBranding(new[] { "imageUrl", "/nitro-assets/roomads/a.png", "clickUrl", "", key, value }));
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("/nitro-assets/roomads/a.png")]
    [InlineData("https://example.invalid/a")]
    [InlineData("data:text/html,AAAA")]
    public void BrandingRefusesANonBlankClickUrlValue(string value)
    {
        Assert.True(FurniExtraData.RejectsBranding(new[] { "imageUrl", "/nitro-assets/roomads/a.png", "clickUrl", value }));
    }

    [Fact]
    public void OnlyOneImagePairIsAllowedAndOtherKeysKeepTheClientImageRules()
    {
        Assert.True(FurniExtraData.RejectsBranding(new[] { "imageUrl", "/nitro-assets/roomads/a.png", "imageUrl", "/nitro-assets/roomads/b.png" }));
        Assert.True(FurniExtraData.RejectsBranding(new[] { "imageUrl", "/nitro-assets/roomads/a.png", "url", "/nitro-assets/roomads/a.png" }));
        Assert.True(FurniExtraData.RejectsBranding(new[] { "ImageUrl", "/nitro-assets/roomads/a.png" }));
        Assert.True(FurniExtraData.RejectsBranding(new[] { "imageUrl", "/nitro-assets/roomads/a.png", "offsetX", "https://example.invalid/a.png" }));
        Assert.True(FurniExtraData.RejectsBranding(new[] { "imageUrl", "/nitro-assets/roomads/a.png", "offsetX", "1\nimageUrl\thttps://example.invalid/a.png" }));
        Assert.True(FurniExtraData.RejectsBranding(new[] { "imageUrl" }));
    }

    [Fact]
    public void GiftAndOtherClientImageChecksAreUnchanged()
    {
        Assert.True(FurniExtraData.RejectsClientImage(new[] { "imageUrl", "/nitro-assets/roomads/a.png" }));
    }
}
