/* ============================================================================
 * SparkFlow File Header
 * File: Libraries/SettingsStore/Models/AppSettings.cs
 * Purpose: Library component: AppSettings.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using System.Collections.Generic;
using Newtonsoft.Json;

namespace SettingsStore.Models
{
    /// <summary>
    /// Application settings model.
    /// - Persisted properties are serialized to disk.
    /// - [JsonIgnore] properties are runtime-only (not persisted).
    /// </summary>
    public sealed class AppSettings
    {
        // =====================================================================
        // PUBLIC (visible to user) - UI / Preferences
        // =====================================================================

        [JsonIgnore] public int SizeWidth { get; set; } = 1280;
        [JsonIgnore] public int SizeHeight { get; set; } = 620;

        public string Language { get; set; } = "en";

        /// <summary>
        /// Preferred emulator name (UI default). The platform is emulator-agnostic.
        /// </summary>
        public string Emulator { get; set; } = "LDPlayer";

        [JsonIgnore] public bool FirstSetup { get; set; } = true;

        /// <summary>
        /// Selected profile must persist.
        /// </summary>
        public string SelectedProfile { get; set; } = string.Empty;

        [JsonIgnore] public bool AutoUpdate { get; set; } = true;

        /// <summary>
        /// Global Runner behavior (persisted).
        /// </summary>
        public bool AutoRestartEnabled { get; set; } = true;

        // =====================================================================
        // BRANDING / META (client-facing; not persisted)
        // =====================================================================

        [JsonIgnore] public string CompanyTitle { get; set; } = "Fluentbyte Inc";
        [JsonIgnore] public string AppTitle { get; set; } = "SparkFlow Automation Platform";

        /// <summary>
        /// App version display string. Prefer sourcing from IAppInfoService at runtime.
        /// </summary>
        [JsonIgnore] public string Version { get; set; } = "1.0.0";

        /// <summary>
        /// License is treated as sensitive; do not persist here.
        /// </summary>
        [JsonIgnore] public string License { get; set; } = string.Empty;

        // =====================================================================
        // WINDOW / ANDROID COUPLING (persisted)
        // =====================================================================

        /// <summary>
        /// If true: keep emulator WINDOW client area matched to Android resolution.
        /// Recommended: avoids template / click scaling issues.
        /// </summary>
        public bool LockWindowClientToAndroidResolution { get; set; } = true;

        // =====================================================================
        // ACCOUNTS UI - WINDOWS WINDOW SIZE (persisted for UX)
        // NOTE:
        // - These represent desired CLIENT AREA size when applying via
        //   WindowsService.ApplyWindowClientRect* (NOT outer window size).
        // - When LockWindowClientToAndroidResolution = true, these are treated
        //   as UI/legacy values and will be normalized to match Android.
        // =====================================================================

        public int EmulatorWindowX { get; set; } = 0;
        public int EmulatorWindowY { get; set; } = 0;

        // ✅ Keep defaults in sync with Android (for UI consistency)
        public int EmulatorWindowWidth { get; set; } = 400;
        public int EmulatorWindowHeight { get; set; } = 652;

        public bool EmulatorWindowMaximized { get; set; } = false;
        public bool EmulatorWindowMinimized { get; set; } = false;

        // =====================================================================
        // EMULATOR ANDROID DISPLAY (persisted)  ✅ SOURCE OF TRUTH
        // =====================================================================

        /// <summary>
        /// Requested Android resolution (internal emulator display).
        /// Keep stable for template-based image recognition.
        /// </summary>
        public int EmulatorAndroidWidth { get; set; } = 400;
        public int EmulatorAndroidHeight { get; set; } = 652;
        public int EmulatorAndroidDpi { get; set; } = 120;

        /// <summary>
        /// Enable LDPlayer ADB once globally.
        /// Keep FALSE by default so the "enable once" logic can run.
        /// </summary>
        public bool LdAdbEnabledOnce { get; set; } = false;

        // =====================================================================
        // LAST SELECTIONS (runtime-only)
        // =====================================================================

        [JsonIgnore] public string LastSelectedAppUid { get; set; } = "unspecified";
        [JsonIgnore] public string LastSelectedEmulatorUid { get; set; } = "LDPlayer";
        [JsonIgnore] public int LastSelectedInstance { get; set; }
        [JsonIgnore] public int MaxInstances { get; set; } = 999;

        // =====================================================================
        // LOGS & SESSIONS (runtime-only)
        // =====================================================================

        [JsonIgnore] public int MaxDisplayedLogs { get; set; } = 1000;
        [JsonIgnore] public int ScreenshotCache { get; set; } = 50;
        [JsonIgnore] public int Sessions { get; set; } = 1;

        // =====================================================================
        // SERVER (runtime-only)
        // =====================================================================

        [JsonIgnore] public bool UseWebServer { get; set; } = true;
        [JsonIgnore] public int WebServerPort { get; set; } = 5508;

        // =====================================================================
        // PATHS & PROFILES (runtime-only)
        // =====================================================================

        [JsonIgnore] public string EmulatorPathCollection { get; set; } = string.Empty;
        [JsonIgnore] public bool TrialMode { get; set; }

        // =====================================================================
        // DEBUG (runtime-only)
        // =====================================================================

        [JsonIgnore] public bool ScriptDebugLogging { get; set; } = true;
        [JsonIgnore] public bool FastBootSessions { get; set; }
        [JsonIgnore] public bool ImageDebugging { get; set; }
        [JsonIgnore] public string LdPath { get; set; } = string.Empty;
        [JsonIgnore] public bool LocalDebugging { get; set; }
        [JsonIgnore] public bool SimulateActionDebug { get; set; }
        [JsonIgnore] public bool SimulateAppDebug { get; set; }

        // =====================================================================
        // UPDATE INTERNALS (runtime-only)
        // =====================================================================

        [JsonIgnore] public bool UpgradeRequired { get; set; } = true;

        // =====================================================================
        // TIMEOUTS (persisted)
        // =====================================================================

        public int TimeoutEmulatorStart { get; set; } = 45;
        public int TimeoutAndroidBoot { get; set; } = 240;
        public int TimeoutFirstImage { get; set; } = 45;
        public int TimeoutActivity { get; set; } = 40;
        public int TimeoutSameScreen { get; set; } = 180;
        public int TimeoutMaxAction { get; set; } = 30;
        public int BootingDelay { get; set; } = 10;

        // =====================================================================
        // HEALTH / AUTO-FIX TIMEOUTS (persisted)
        // =====================================================================

        public int TimeoutAutoBindSerialSec { get; set; } = 45;
        public int BootingDelayAutoBindSec { get; set; } = 10;

        // =====================================================================
        // NORMALIZATION (optional helper)
        // =====================================================================

        /// <summary>
        /// Keeps Window client size consistent with Android resolution when lock is enabled.
        /// Safe to call before Save() or before applying policies.
        /// </summary>
        public void Normalize()
        {
            if (LockWindowClientToAndroidResolution)
            {
                EmulatorWindowWidth = EmulatorAndroidWidth;
                EmulatorWindowHeight = EmulatorAndroidHeight;
            }
        }

        // =====================================================================
        // VALIDATION
        // =====================================================================

        public IReadOnlyList<string> Validate()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(Language))
                errors.Add("Language cannot be empty.");

            if (string.IsNullOrWhiteSpace(Emulator))
                errors.Add("Emulator cannot be empty.");

            // Android display sanity (source of truth)
            if (EmulatorAndroidWidth < 200 || EmulatorAndroidWidth > 4000)
                errors.Add("EmulatorAndroidWidth out of range (200..4000).");

            if (EmulatorAndroidHeight < 200 || EmulatorAndroidHeight > 4000)
                errors.Add("EmulatorAndroidHeight out of range (200..4000).");

            if (EmulatorAndroidDpi < 120 || EmulatorAndroidDpi > 640)
                errors.Add("EmulatorAndroidDpi out of range (120..640).");

            // Window size sanity (client area)
            if (EmulatorWindowWidth < 200 || EmulatorWindowWidth > 4000)
                errors.Add("EmulatorWindowWidth out of range (200..4000).");

            if (EmulatorWindowHeight < 200 || EmulatorWindowHeight > 4000)
                errors.Add("EmulatorWindowHeight out of range (200..4000).");

            if (LockWindowClientToAndroidResolution)
            {
                if (EmulatorAndroidWidth < 200 || EmulatorAndroidHeight < 200)
                    errors.Add("Android resolution must be valid when LockWindowClientToAndroidResolution is enabled.");
            }

            if (WebServerPort < 1 || WebServerPort > 65535)
                errors.Add("WebServerPort out of range (1..65535).");

            if (TimeoutEmulatorStart < 5) errors.Add("TimeoutEmulatorStart too small (min 5).");
            if (TimeoutAndroidBoot < 10) errors.Add("TimeoutAndroidBoot too small (min 10).");
            if (TimeoutFirstImage < 5) errors.Add("TimeoutFirstImage too small (min 5).");
            if (TimeoutActivity < 5) errors.Add("TimeoutActivity too small (min 5).");
            if (TimeoutSameScreen < 10) errors.Add("TimeoutSameScreen too small (min 10).");
            if (TimeoutMaxAction < 5) errors.Add("TimeoutMaxAction too small (min 5).");

            if (TimeoutAutoBindSerialSec < 5) errors.Add("TimeoutAutoBindSerialSec too small (min 5).");
            if (BootingDelay < 0) errors.Add("BootingDelay cannot be negative.");

            return errors;
        }
    }
}