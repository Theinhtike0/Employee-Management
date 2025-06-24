using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using HR_Products.Models.Entitites;

public class PensionRequestViewModel
{
    public PensionRequest Pension { get; set; }

    [Display(Name = "Attachment File")]
    public IFormFile AttachFile { get; set; }
}
