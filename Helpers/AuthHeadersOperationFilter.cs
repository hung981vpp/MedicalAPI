using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace MedicalRecordService.Helpers;

public sealed class AuthHeadersOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        operation.Parameters ??= [];
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "X-User-Id",
            In = ParameterLocation.Header,
            Required = false,
            Description = "Demo auth user id, example: 7 for Doctor or 25 for Patient",
            Schema = new OpenApiSchema { Type = "string" }
        });
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "X-Role",
            In = ParameterLocation.Header,
            Required = false,
            Description = "Role: Admin, Doctor, Nurse, Receptionist, Patient",
            Schema = new OpenApiSchema { Type = "string" }
        });
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "X-Patient-Id",
            In = ParameterLocation.Header,
            Required = false,
            Description = "Required when X-Role is Patient",
            Schema = new OpenApiSchema { Type = "string" }
        });
    }
}
