# DeviceBindingLib (SparkFlow v1)

Stable Profile ↔ Device binding for SparkFlow.

## Identity Strategy
- Primary: SparkFlow GUID stored on device
- Fallback: Android ID
- Auto-Repair: if GUID missing but AndroidId matches, GUID is recreated

UI never reads device identity directly.
Core resolves targets before Runner execution.
