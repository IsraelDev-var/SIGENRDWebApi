using System.Text.RegularExpressions;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;
using SIGENRD.Core.Application.Interfaces.Services;

namespace SIGENRD.Infrastructure.Shared.Services
{
    public class StorageSettings
    {
        public const string SectionName = "Storage";

        /// <summary>"AzureBlob" (producción) o "Local" (desarrollo y pruebas).</summary>
        public string Provider { get; set; } = "AzureBlob";

        /// <summary>Carpeta de los archivos con el proveedor Local.</summary>
        public string LocalPath { get; set; } = "App_Data/documents";

        public AzureBlobSettings AzureBlob { get; set; } = new();
    }

    public class AzureBlobSettings
    {
        /// <summary>Cadena de conexión de la cuenta de almacenamiento. Es un secreto (Key Vault).</summary>
        public string ConnectionString { get; set; } = string.Empty;

        /// <summary>Contenedor privado: sin acceso público; las descargas pasan por la API.</summary>
        public string ContainerName { get; set; } = "expedientes";
    }

    internal static partial class StorageKeys
    {
        // Las claves las arma la aplicación ("requests/12/abc.pdf"); cualquier otra cosa es un error de programación.
        [GeneratedRegex(@"^[A-Za-z0-9][A-Za-z0-9_\-]*(/[A-Za-z0-9][A-Za-z0-9_\-.]*)*$")]
        private static partial Regex Valid();

        public static string Check(string key) =>
            Valid().IsMatch(key) && !key.Contains("..") ? key : throw new ArgumentException("Clave de almacenamiento no válida: " + key);
    }

    /// <summary>Archivos en disco. Solo para desarrollo y pruebas: en un contenedor el disco no es persistente.</summary>
    public class LocalFileStorage : IFileStorage
    {
        private readonly string _root;

        public LocalFileStorage(IOptions<StorageSettings> settings)
        {
            _root = Path.GetFullPath(settings.Value.LocalPath);
            Directory.CreateDirectory(_root);
        }

        private string PathFor(string key)
        {
            var path = Path.GetFullPath(Path.Combine(_root, StorageKeys.Check(key)));
            if (!path.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
                throw new ArgumentException("Clave de almacenamiento fuera de la carpeta: " + key);
            return path;
        }

        public async Task SaveAsync(string key, Stream content, string contentType, CancellationToken cancellationToken = default)
        {
            var path = PathFor(key);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            // Se escribe a un temporal y se mueve: nunca queda un archivo a medias con el nombre final.
            var temp = path + ".tmp-" + Guid.NewGuid().ToString("N");
            await using (var file = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true))
                await content.CopyToAsync(file, cancellationToken);
            File.Move(temp, path, overwrite: false);
        }

        public Task<Stream?> OpenReadAsync(string key, CancellationToken cancellationToken = default)
        {
            var path = PathFor(key);
            Stream? stream = File.Exists(path)
                ? new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true)
                : null;
            return Task.FromResult(stream);
        }

        public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
        {
            var path = PathFor(key);
            if (File.Exists(path)) File.Delete(path);
            return Task.CompletedTask;
        }
    }

    /// <summary>Archivos en Azure Blob Storage (RF-35), en un contenedor privado.</summary>
    public class AzureBlobFileStorage : IFileStorage
    {
        private readonly BlobContainerClient _container;
        private readonly Lazy<Task> _ensureContainer;

        public AzureBlobFileStorage(IOptions<StorageSettings> settings)
        {
            var blob = settings.Value.AzureBlob;
            if (string.IsNullOrWhiteSpace(blob.ConnectionString))
                throw new InvalidOperationException("Storage:AzureBlob:ConnectionString no está configurada.");

            _container = new BlobContainerClient(blob.ConnectionString, blob.ContainerName);
            _ensureContainer = new Lazy<Task>(() => _container.CreateIfNotExistsAsync(PublicAccessType.None));
        }

        public async Task SaveAsync(string key, Stream content, string contentType, CancellationToken cancellationToken = default)
        {
            await _ensureContainer.Value;
            await _container.GetBlobClient(StorageKeys.Check(key)).UploadAsync(content, new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = contentType },
                // Una clave nueva por archivo: si ya existe, algo anda mal y no se sobrescribe.
                Conditions = new BlobRequestConditions { IfNoneMatch = ETag.All }
            }, cancellationToken);
        }

        public async Task<Stream?> OpenReadAsync(string key, CancellationToken cancellationToken = default)
        {
            await _ensureContainer.Value;
            try
            {
                var response = await _container.GetBlobClient(StorageKeys.Check(key)).DownloadStreamingAsync(cancellationToken: cancellationToken);
                return response.Value.Content;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return null;
            }
        }

        public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
        {
            await _ensureContainer.Value;
            await _container.GetBlobClient(StorageKeys.Check(key)).DeleteIfExistsAsync(cancellationToken: cancellationToken);
        }
    }
}
