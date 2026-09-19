using System.Net;

namespace SFMA_API.Models.ServiceModel
{
    public class ServiceResponse<T>
    {
        public bool Success { get; set; } = true;
        public HttpStatusCode HttpStatusCode { get; set; } = HttpStatusCode.OK;
        public string? Message { get; set; }
        public T? Data { get; set; }
    }

    public class ServiceResponse
    {
        public bool Success { get; set; } = true;
        public HttpStatusCode HttpStatusCode { get; set; } = HttpStatusCode.OK;
        public string? Message { get; set; }
        public virtual object? Data { get; set; }
    }
}
