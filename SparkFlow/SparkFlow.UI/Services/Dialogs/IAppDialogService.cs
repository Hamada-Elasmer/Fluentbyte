/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlow.UI/Services/Dialogs/IAppDialogService.cs
 * Purpose: UI component: IAppDialogService.
 * Notes:
 *  - UI-side dialog service (Suki) that includes both core dialogs and content dialogs.
 * ============================================================================ */

using System.Threading.Tasks;
using SparkFlow.Abstractions.Services.Dialogs;
using SparkFlow.Domain.Models.Dialogs;

namespace SparkFlow.UI.Services.Dialogs;

public interface IAppDialogService : IDialogService
{
    Task<AppDialogResult> ShowAccountHealthCheckAsync(string profileId, string primaryText = "Close");
    Task<AppDialogResult> ShowGameInstallAsync(string profileId, string primaryText = "Close");
}