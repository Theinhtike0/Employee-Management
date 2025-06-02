namespace HR_Products.ViewModels
{
    public class LeaveApprovalViewModel
    {
        public int Id { get; set; }
        public string EmployeeName { get; set; }
        public string LeaveTypeName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string DurationType { get; set; }
        public decimal Duration { get; set; }
        public string Reason { get; set; }
        public DateTime RequestedAt { get; set; }
        public string ApproverComments { get; set; }

        public string AttachmentPath { get; set; }
        public string AttachmentFileName { get; set; }
        public string AttachmentContentType { get; set; }
    }
}
