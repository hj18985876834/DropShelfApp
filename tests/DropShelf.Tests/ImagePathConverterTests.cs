using System.Globalization;
using System.IO;
using DropShelf.App.Converters;

namespace DropShelf.Tests;

[TestClass]
public sealed class ImagePathConverterTests
{
    [TestMethod]
    public void Convert_ReturnsNullForInvalidImageFile()
    {
        using var tempDirectory = new TempDirectory();
        var invalidImagePath = Path.Combine(tempDirectory.Path, "invalid.png");
        File.WriteAllText(invalidImagePath, "not an image");
        var converter = new ImagePathConverter();

        var result = converter.Convert(invalidImagePath, typeof(object), null, CultureInfo.InvariantCulture);

        Assert.IsNull(result);
    }
}
