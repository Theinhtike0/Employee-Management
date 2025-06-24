using System.Collections.Generic;
using HR_Products.Models.Entitites;

public class PensionListViewModel
{
    public List<PensionRequest> PensionRequests { get; set; } = new List<PensionRequest>();
    public bool IsAdmin { get; set; }
    public string? CurrentUserName { get; set; }
}