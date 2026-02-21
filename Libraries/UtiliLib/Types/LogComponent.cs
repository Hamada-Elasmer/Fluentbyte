/* ============================================================================
 * SparkFlow File Header
 * File: Libraries/UtiliLib/Types/LogComponent.cs
 * Purpose: Logging component classifier (Component-based logging).
 * Notes:
 *  - Used for routing logs into separate files for fast debugging.
 * ============================================================================ */

namespace UtiliLib.Types;

public enum LogComponent
{
    System,
    Api,
    Runner,
    Adb,
    Emulator,
    Health,
    Ui,
    Game,
    Notification,
    Network,
    Script,
    Hint,
    Unknown
}