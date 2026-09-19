using Newtonsoft.Json;

namespace SFMA_API.Models.Dtos.Response
{
    public class SuccessResponse
    {
        public bool Success { get; set; } = true;
        public object? Data { get; set; }
        public string? Message { get; set; }
    }

    public class ResponseV2<T>
    {
        public bool Success { get; set; } = true;
        public string? Message { get; set; }
        public T? Data { get; set; }
    }

    public class ResponseV2
    {
        public bool Success { get; set; } = true;
        public string? Message { get; set; }
    }

    public class ErrorResponse
    {
        public int Status { get; set; }
        public bool Success { get; set; } = false;
        public string? Message { get; set; }
        public object? Data { get; set; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
