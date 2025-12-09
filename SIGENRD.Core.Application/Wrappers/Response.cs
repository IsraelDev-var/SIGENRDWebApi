using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIGENRD.Core.Application.Wrappers
{
    public class Response<T>
    {
        public bool Succeeded { get; set; }
        public string? Message { get; set; }
        public List<string>? Errors { get; set; }
        public T? Data { get; set; }

        public Response()
        {
        }

        // Constructor para Respuestas Exitosas (Success)
        public Response(T data, string? message = null)
        {
            Succeeded = true;
            Message = message;
            Data = data;
        }

        // Constructor para Respuestas Fallidas (Fail)
        public Response(string message)
        {
            Succeeded = false;
            Message = message;
        }
    }
}
