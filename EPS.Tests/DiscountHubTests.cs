using EPS.SignalR;
using Microsoft.Extensions.Options;
using Moq;

namespace EPS.Tests;

public class DiscountHubTests
{
    [Fact]
    public async Task GenerateCodes_ShouldGenerateCorrectNumberOfCodes()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();

        var options = new Mock<IOptions<DiscountOptions>>();
        options.Setup(o => o.Value).Returns(new DiscountOptions
        {
            MaxCodesPerRequest = 10,
            MinCodeLength = 5,
            MaxCodeLength = 10,
            StoragePath = tempFile
        });

        var hub = new DiscountHub(options.Object);

        var result = await hub.GenerateCodes(5, 6);

        Assert.True(result);

        var codesJson = File.ReadAllText(tempFile);
        Assert.False(string.IsNullOrWhiteSpace(codesJson));
        Assert.Contains("\"", codesJson); 
    }

    [Fact]
    public async Task GenerateCodes_ShouldFail_WhenCountExceedsLimit()
    {
        var hub = CreateHub(Path.GetTempFileName());
        var result = await hub.GenerateCodes(100, 6);
        Assert.False(result);
    }

    [Fact]
    public async Task GenerateCodes_ShouldFail_WhenLengthIsOutOfRange()
    {
        var hub = CreateHub(Path.GetTempFileName());

        var tooShort = await hub.GenerateCodes(1, 2);
        var tooLong = await hub.GenerateCodes(1, 20);

        Assert.False(tooShort);
        Assert.False(tooLong);
    }

    [Fact]
    public async Task UseCode_ShouldRemoveCode_IfExists()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();

        var options = new Mock<IOptions<DiscountOptions>>();
        options.Setup(o => o.Value).Returns(new DiscountOptions
        {
            MaxCodesPerRequest = 10,
            MinCodeLength = 5,
            MaxCodeLength = 10,
            StoragePath = tempFile
        });

        var hub = new DiscountHub(options.Object);

        await hub.GenerateCodes(1, 6);

        var codesJson = File.ReadAllText(tempFile);
        var codes = System.Text.Json.JsonSerializer.Deserialize<HashSet<string>>(codesJson);
        Assert.NotNull(codes);
        Assert.NotEmpty(codes);

        var code = codes.First();

        var result = await hub.UseCode(code);

        Assert.Equal(1, result);

        var updatedJson = await File.ReadAllTextAsync(tempFile);
        var updatedCodes = System.Text.Json.JsonSerializer.Deserialize<HashSet<string>>(updatedJson);
        Assert.DoesNotContain(code, updatedCodes);
    }

    [Fact]
    public async Task UseCode_ShouldReturnZero_IfCodeNotFound()
    {
        var hub = CreateHub(Path.GetTempFileName());
        var result = await hub.UseCode("INVALID");
        Assert.Equal(0, result);
    }
    
    //Helpers
    
    private DiscountOptions GetDefaultOptions() => new DiscountOptions
    {
        MaxCodesPerRequest = 10,
        MinCodeLength = 5,
        MaxCodeLength = 10
    };

    private DiscountHub CreateHub(string storagePath)
    {
        var options = new Mock<IOptions<DiscountOptions>>();
        options.Setup(o => o.Value).Returns(GetDefaultOptions());

        var hub = new DiscountHub(options.Object);
        typeof(DiscountHub).GetField("_storagePath",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
            ?.SetValue(null, storagePath);
        return hub;
    }
}