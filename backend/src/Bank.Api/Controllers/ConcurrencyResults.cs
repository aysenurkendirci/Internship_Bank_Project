using Microsoft.AspNetCore.Mvc;

namespace Bank.Api.Controllers;

internal static class ConcurrencyResults
{
    /// <summary>
    /// 428 for an update sent without If-Match. Without the version the client edited, the API cannot tell
    /// whether it is about to overwrite a change the client never saw.
    /// </summary>
    public static ObjectResult VersionRequired(this ControllerBase controller)
        => controller.Problem(
            statusCode: StatusCodes.Status428PreconditionRequired,
            title: "Kayıt sürümü gerekli",
            detail: "Güncelleme için If-Match başlığında kaydın version değeri gönderilmelidir.");
}
