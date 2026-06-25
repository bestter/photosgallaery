using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;

namespace PhotoAppApi.DTOs
{
    public class UploadRequestDto
    {
        public IList<IFormFile>? Files { get; set; }
        public string? Tags { get; set; }
        public Guid? GroupId { get; set; }
        public bool IncludeGps { get; set; } = true;
    }
}
