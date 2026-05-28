using System.Security.Claims;

namespace MedicalRecordService.Helpers;

public static class JwtClaimsHelper
{
    public static int GetUserId(ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub") ?? "0";
        return int.TryParse(value, out var userId) ? userId : 0;
    }

    public static int? GetPatientId(ClaimsPrincipal user)
    {
        var value = user.FindFirstValue("PatientId") ?? user.FindFirstValue("patient_id");
        return int.TryParse(value, out var patientId) ? patientId : null;
    }

    public static bool IsPatientAllowed(ClaimsPrincipal user, int patientId)
    {
        if (!user.IsInRole("Patient"))
        {
            return true;
        }

        return GetPatientId(user) == patientId;
    }
}
