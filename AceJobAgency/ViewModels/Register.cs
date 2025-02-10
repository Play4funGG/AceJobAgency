using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace AceJobAgency.ViewModels
{
    public class Register
    {
        [Required(ErrorMessage = "First name is required.")]
        [DataType(DataType.Text)]
        [RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "First name can only contain letters and spaces.")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Last name is required.")]
        [DataType(DataType.Text)]
        [RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "Last name can only contain letters and spaces.")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Gender is required.")]
        [RegularExpression(@"^(Male|Female)$", ErrorMessage = "Gender must be either 'Male' or 'Female'.")]
        public string Gender { get; set; }

        [Required(ErrorMessage = "NRIC is required.")]
        [DataType(DataType.Text)]
        [RegularExpression(@"^[STFG]\d{7}[A-Z]$", ErrorMessage = "Invalid NRIC format.")]
        public string NRIC { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [DataType(DataType.EmailAddress)]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 12, ErrorMessage = "Password must be at least 12 characters long.")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{12,}$",
            ErrorMessage = "Password must include uppercase, lowercase, number, and special character.")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Confirm password is required.")]
        [DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "Password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Date of birth is required.")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        [Range(typeof(DateTime), "1900-01-01", "2023-01-01", ErrorMessage = "You must be at least 18 years old.")]
        public DateTime DateOfBirth { get; set; }

        [Required(ErrorMessage = "Resume is required.")]
        [DataType(DataType.Upload)]
        [AllowedExtensions(new[] { ".docx", ".pdf" }, ErrorMessage = "Only .docx and .pdf files are allowed.")]
        [MaxFileSize(5 * 1024 * 1024, ErrorMessage = "File size cannot exceed 5MB.")]
        public IFormFile Resume { get; set; }

        [Required(ErrorMessage = "WhoAmI description is required.")]
        [DataType(DataType.MultilineText)]
        [StringLength(500, MinimumLength = 10, ErrorMessage = "Description must be between 10 and 500 characters.")]
        public string WhoAmI { get; set; }
    }

    // Custom Validation Attribute for File Extensions
    public class AllowedExtensionsAttribute : ValidationAttribute
    {
        private readonly string[] _extensions;

        public AllowedExtensionsAttribute(string[] extensions)
        {
            _extensions = extensions;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value is IFormFile file)
            {
                var extension = Path.GetExtension(file.FileName).ToLower();
                if (!_extensions.Contains(extension))
                {
                    return new ValidationResult(ErrorMessage ?? "Invalid file type.");
                }
            }
            return ValidationResult.Success;
        }
    }

    // Custom Validation Attribute for File Size
    public class MaxFileSizeAttribute : ValidationAttribute
    {
        private readonly int _maxFileSize;

        public MaxFileSizeAttribute(int maxFileSize)
        {
            _maxFileSize = maxFileSize;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value is IFormFile file)
            {
                if (file.Length > _maxFileSize)
                {
                    return new ValidationResult(ErrorMessage ?? $"File size exceeds {_maxFileSize / (1024 * 1024)}MB.");
                }
            }
            return ValidationResult.Success;
        }
    }
}