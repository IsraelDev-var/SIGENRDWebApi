using SIGENRD.Core.Application.Exceptions;
using SIGENRD.Core.Application.Wrappers;
using System.Net;
using System.Text.Json;

namespace SIGENRDWebApi.Middlewares
{
    public class ErrorHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlerMiddleware> _logger;

        public ErrorHandlerMiddleware(RequestDelegate next, ILogger<ErrorHandlerMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                // Intenta ejecutar la solicitud normal
                await _next(context);
            }
            catch (Exception error)
            {
                // Si falla, atrapa el error
                var response = context.Response;
                response.ContentType = "application/json";

                // Creamos el wrapper de respuesta fallida
                var responseModel = new Response<string>()
                {
                    Succeeded = false,
                    Message = error.Message
                };

                // Clasificamos el error
                switch (error)
                {
                    case ApiException e:
                        // Error de negocio controlado (400 Bad Request)
                        response.StatusCode = (int)HttpStatusCode.BadRequest;
                        _logger.LogWarning($"⚠️ Error de API: {e.Message}");
                        break;

                    case KeyNotFoundException e:
                        // No encontrado (404 Not Found)
                        response.StatusCode = (int)HttpStatusCode.NotFound;
                        responseModel.Message = "El recurso solicitado no fue encontrado.";
                        _logger.LogWarning($"🔍 No encontrado: {e.Message}");
                        break;
                    case ValidationException e:
                        // Error de validación (400 Bad Request)
                        response.StatusCode = (int)HttpStatusCode.BadRequest;
                        responseModel.Message = "Se encontraron errores de validación."; // Mensaje general
                        responseModel.Errors = e.Errors; // 👈 Aquí pasamos la lista detallada de errores
                        _logger.LogWarning("⚠️ Validación fallida.");
                        break;

                    default:
                        // Error inesperado (500 Internal Server Error)
                        response.StatusCode = (int)HttpStatusCode.InternalServerError;
                        // En producción, no muestres 'error.Message' real al usuario por seguridad
                        responseModel.Message = "Ocurrió un error interno en el servidor.";
                        _logger.LogError(error, "🚨 Error Crítico no manejado");
                        break;
                }

                // Devolvemos el JSON
                var result = JsonSerializer.Serialize(responseModel);
                await response.WriteAsync(result);
            }
        }
    }
}
