using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using HR_Products.Data;
using HR_Products.Models.Entitites;
using HR_Products.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace HR_Products.Attributes
{
    public class RequiredIfLeaveTypeAttribute : ValidationAttribute
    {
        private readonly string[] _requiredLeaveTypes;

        public RequiredIfLeaveTypeAttribute(params string[] requiredLeaveTypes)
        {
            _requiredLeaveTypes = requiredLeaveTypes;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            // Resolve DbContext from DI
            var dbContext = (DbContext)validationContext.GetService(typeof(AppDbContext));
            if (dbContext == null)
            {
                throw new InvalidOperationException("DbContext not available");
            }

            var model = (LeaveRequestViewModel)validationContext.ObjectInstance;

            var leaveType = dbContext.Set<LeaveType>()
                .AsNoTracking()
                .FirstOrDefault(lt => lt.LEAV_TYPE_ID == model.LeaveTypeId);

            if (leaveType == null)
            {
                return ValidationResult.Success;
            }

            if (_requiredLeaveTypes.Contains(leaveType.LEAV_TYPE_NAME) && value == null)
            {
                return new ValidationResult(ErrorMessage ?? "Attachment file is required for this leave type");
            }

            return ValidationResult.Success;
        }
    }
}