namespace SIGENRD.Core.Application.Interfaces.Services
{
    /// <summary>
    /// Almacén de archivos de los expedientes (RF-35): Azure Blob Storage en producción, disco local en desarrollo.
    /// Las claves las genera la aplicación (nunca el usuario) y no son públicas: toda descarga pasa por la API (RN-22).
    /// </summary>
    public interface IFileStorage
    {
        Task SaveAsync(string key, Stream content, string contentType, CancellationToken cancellationToken = default);

        /// <summary>null si el archivo no existe.</summary>
        Task<Stream?> OpenReadAsync(string key, CancellationToken cancellationToken = default);

        Task DeleteAsync(string key, CancellationToken cancellationToken = default);
    }

    /// <summary>Datos que muestra el certificado de aprobación (RN-14).</summary>
    public record ApprovalCertificateData(
        string RequestCode,
        string RequestType,
        string ApplicantName,
        string? ProjectAddress,
        string DistributorName,
        string TransformerCode,
        decimal ApprovedCapacityKva,
        DateTime ApprovedAt,
        string ReviewerName);

    public interface ICertificateGenerator
    {
        /// <summary>El certificado en PDF.</summary>
        byte[] Generate(ApprovalCertificateData data);
    }
}
