using System.Net;
using Codebelt.Extensions.Xunit;
using Cuemon.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders;
using Xunit;

namespace Codebelt.Cdn.Origin;

public class CaseInsensitivePathTest : Test
{
    private static readonly CaseVariant[] CaseVariants =
    [
        new(
            "PascalCase/AssetFile.txt",
            [
                "/PascalCase/AssetFile.txt",
                "/pascalcase/assetfile.txt",
                "/PASCALCASE/ASSETFILE.TXT",
                "/pAsCaLcAsE/aSsEtFiLe.TxT"
            ],
            "PascalCase asset"),
        new(
            "camelCase/assetFile.txt",
            [
                "/camelCase/assetFile.txt",
                "/camelcase/assetfile.txt",
                "/CAMELCASE/ASSETFILE.TXT",
                "/cAmElCaSe/aSsEtFiLe.TxT"
            ],
            "camelCase asset"),
        new(
            "mixedCase/MiXeDFile.txt",
            [
                "/mixedCase/MiXeDFile.txt",
                "/mixedcase/mixedfile.txt",
                "/MIXEDCASE/MIXEDFILE.TXT",
                "/mIxEdCaSe/mIxEdFiLe.TxT"
            ],
            "mixedCase asset"),
        new(
            "lowercase/assetfile.txt",
            [
                "/lowercase/assetfile.txt",
                "/LOWERCASE/ASSETFILE.TXT",
                "/LowerCase/AssetFile.TxT",
                "/lOwErCaSe/aSsEtFiLe.tXt"
            ],
            "lowercase asset"),
        new(
            "UPPERCASE/ASSETFILE.TXT",
            [
                "/UPPERCASE/ASSETFILE.TXT",
                "/uppercase/assetfile.txt",
                "/UpperCase/AssetFile.TxT",
                "/uPpErCaSe/AsSeTfIlE.tXt"
            ],
            "UPPERCASE asset")
    ];

    public CaseInsensitivePathTest(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public async Task Get_ShouldServeEveryCasingVariant_WhenCanonicalFilesUseDifferentCasing()
    {
        await using var application = new CdnOriginTestApplication(TestOutput);

        foreach (CaseVariant variant in CaseVariants)
        {
            WriteFile(application.Content.Root, variant.ActualPath, variant.Content);
        }

        using var client = application.CreateClient();
        using var legacy = new LegacyCaseInsensitivePhysicalFileProvider(application.Content.Root);
        using var modern = new PortablePhysicalFileProvider(application.Content.Root);

        foreach (CaseVariant variant in CaseVariants)
        {
            foreach (string requestedPath in variant.RequestPaths)
            {
                IFileInfo legacyFile = legacy.GetFileInfo(requestedPath);
                IFileInfo modernFile = modern.GetFileInfo(requestedPath);

                Assert.True(legacyFile.Exists, $"Legacy provider did not resolve '{requestedPath}'.");
                Assert.True(modernFile.Exists, $"Replacement provider did not resolve '{requestedPath}'.");
                Assert.Equal(legacyFile.Name, modernFile.Name);
                Assert.Equal(legacyFile.Length, modernFile.Length);
                Assert.Equal(legacyFile.PhysicalPath, modernFile.PhysicalPath);

                using var response = await client.GetAsync(requestedPath);

                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Equal(variant.Content, await response.Content.ReadAsStringAsync());
            }
        }
    }

    private static void WriteFile(string root, string relativePath, string content)
    {
        string path = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
    }

    private sealed record CaseVariant(string ActualPath, string[] RequestPaths, string Content);
}
