using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HR_Products.Models.Entitites
{
    public class ApprovalWorkflow
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int? LeaveTypeId { get; set; }
        public LeaveType LeaveType { get; set; }
        public bool IsAutoApproved { get; set; }
        public decimal? AutoApproveMaxDays { get; set; }
        public List<ApprovalLevel> Levels { get; set; } = new List<ApprovalLevel>();
    }

    public class ApprovalLevel
    {
        public int Id { get; set; }
        public int LevelNumber { get; set; }
        public string ApproverRole { get; set; }
        public int? ApproverEmpeId { get; set; }
        [ForeignKey("ApproverEmpeId")]
        public EmployeeProfile ApproverProfile { get; set; }

        public int ApprovalWorkflowId { get; set; }
        public ApprovalWorkflow ApprovalWorkflow { get; set; }
    }

}
