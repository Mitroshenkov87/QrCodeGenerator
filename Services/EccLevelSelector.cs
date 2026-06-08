using QRGen = QRCoder.QRCodeGenerator;

namespace QrCodeGenerator.Services;

internal static class EccLevelSelector
{
    public static QRGen.ECCLevel SelectByByteCount(int byteCount) => byteCount switch
    {
        <= 200 => QRGen.ECCLevel.Q,
        <= 800 => QRGen.ECCLevel.M,
        _ => QRGen.ECCLevel.L
    };

    public static IReadOnlyList<QRGen.ECCLevel> GetLevelsToTry(int byteCount)
    {
        var initial = SelectByByteCount(byteCount);

        return initial switch
        {
            QRGen.ECCLevel.Q => [QRGen.ECCLevel.Q, QRGen.ECCLevel.M, QRGen.ECCLevel.L],
            QRGen.ECCLevel.M => [QRGen.ECCLevel.M, QRGen.ECCLevel.L],
            _ => [QRGen.ECCLevel.L]
        };
    }
}