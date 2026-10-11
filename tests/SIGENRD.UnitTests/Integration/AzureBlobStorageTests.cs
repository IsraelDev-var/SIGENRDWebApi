using Microsoft.Extensions.Options;
using SIGENRD.Infrastructure.Shared.Services;
using SIGENRD.UnitTests.Application;

namespace SIGENRD.UnitTests.Integration;

/// <summary>
/// El almacén de producción contra Azurite (emulador oficial de Azure Storage).
/// Se activa con ENERGYMAP_TEST_BLOB = cadena de conexión, por ejemplo "UseDevelopmentStorage=true".
/// Levantarlo: docker run -p 10000:10000 mcr.microsoft.com/azure-storage/azurite azurite-blob --blobHost 0.0.0.0
/// </summary>
public class AzureBlobStorageTests
{
    private const string EnvironmentVariable = "ENERGYMAP_TEST_BLOB";
    private static readonly string? ConnectionString = Environment.GetEnvironmentVariable(EnvironmentVariable);

    private sealed class FactIfBlobAttribute : FactAttribute
    {
        public FactIfBlobAttribute()
        {
            if (string.IsNullOrWhiteSpace(ConnectionString))
                Skip = "Define " + EnvironmentVariable + " para probar Azure Blob Storage (Azurite).";
        }
    }

    private static AzureBlobFileStorage Storage(string container) => new(Options.Create(new StorageSettings
    {
        Provider = "AzureBlob",
        AzureBlob = new AzureBlobSettings { ConnectionString = ConnectionString!, ContainerName = container }
    }));

    [FactIfBlob]
    public async Task Guarda_lee_y_borra_en_un_contenedor_privado()
    {
        var container = "pruebas-" + Guid.NewGuid().ToString("N")[..8];
        var storage = Storage(container);
        var pdf = DocumentComponentsTests.Pdf("/Producer (blob)");

        await storage.SaveAsync("requests/7/doc.pdf", new MemoryStream(pdf), "application/pdf");

        await using (var read = await storage.OpenReadAsync("requests/7/doc.pdf"))
        {
            using var copy = new MemoryStream();
            await read!.CopyToAsync(copy);
            Assert.Equal(pdf, copy.ToArray());
        }

        // El contenedor no es público: sin credenciales no se puede leer.
        var client = new Azure.Storage.Blobs.BlobContainerClient(ConnectionString, container);
        var access = await client.GetAccessPolicyAsync();
        Assert.Equal(Azure.Storage.Blobs.Models.PublicAccessType.None, access.Value.BlobPublicAccess);

        // Una clave nunca se sobrescribe.
        await Assert.ThrowsAsync<Azure.RequestFailedException>(() =>
            storage.SaveAsync("requests/7/doc.pdf", new MemoryStream(pdf), "application/pdf"));

        await storage.DeleteAsync("requests/7/doc.pdf");
        Assert.Null(await storage.OpenReadAsync("requests/7/doc.pdf"));

        await client.DeleteIfExistsAsync();
    }
}
