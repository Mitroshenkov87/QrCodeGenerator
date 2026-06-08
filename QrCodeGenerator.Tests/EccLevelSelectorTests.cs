using QrCodeGenerator.Services;
using QRGen = QRCoder.QRCodeGenerator;

namespace QrCodeGenerator.Tests;

public class EccLevelSelectorTests
{
    [Theory]
    [InlineData(1, QRGen.ECCLevel.Q)]
    [InlineData(200, QRGen.ECCLevel.Q)]
    [InlineData(201, QRGen.ECCLevel.M)]
    [InlineData(800, QRGen.ECCLevel.M)]
    [InlineData(801, QRGen.ECCLevel.L)]
    [InlineData(5000, QRGen.ECCLevel.L)]
    public void SelectByByteCount_ReturnsExpectedLevel(int byteCount, QRGen.ECCLevel expected)
    {
        Assert.Equal(expected, EccLevelSelector.SelectByByteCount(byteCount));
    }

    [Fact]
    public void GetLevelsToTry_ForQLevel_IncludesFallbacks()
    {
        var levels = EccLevelSelector.GetLevelsToTry(100);
        Assert.Equal(
            [QRGen.ECCLevel.Q, QRGen.ECCLevel.M, QRGen.ECCLevel.L],
            levels);
    }
}