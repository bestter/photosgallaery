using System.ComponentModel.DataAnnotations;

﻿namespace PhotoAppApi.dtos
{
    public class UserLoginDto
    {
        [StringLength(100)]
        public string Username { get; set; } = string.Empty;
        [StringLength(100)]
        public string Password { get; set; } = string.Empty;
    }
}
