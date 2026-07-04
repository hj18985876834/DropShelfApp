# Hardware Monitor Floating Window Research

## Topic

Evaluate whether a lightweight Windows hardware monitor floating window is a good MVP direction, and identify feasible system data sources without kernel drivers.

## Feasibility

This is a good candidate for a lightweight Windows daily utility if the MVP stays focused on system-level usage metrics:

* CPU usage
* Memory usage
* Disk usage/activity
* Network upload/download speed
* Floating always-on-top compact window
* Threshold-based color warning

The important scope boundary is to avoid hardware sensor metrics in V1, such as CPU/GPU temperature, fan speed, voltage, and detailed GPU telemetry. Those often require vendor APIs, WMI sensor availability, third-party libraries, admin privileges, or drivers, which would weaken the "no driver, lightweight install" promise.

## Windows API notes

### CPU, disk, memory counters

Windows Performance Counters provide a consistent system data interface for metrics such as CPU, memory, and disk usage. They are suitable for polling usage data in a desktop monitor app.

Source: https://learn.microsoft.com/en-us/windows/win32/perfctrs/performance-counters-portal

### Memory usage

`GlobalMemoryStatusEx` provides current physical and virtual memory status through the `MEMORYSTATUSEX` structure. It is a direct fit for showing memory load and used/available memory.

Sources:

* https://learn.microsoft.com/en-us/windows/win32/api/sysinfoapi/nf-sysinfoapi-globalmemorystatusex
* https://learn.microsoft.com/en-us/windows/win32/api/sysinfoapi/ns-sysinfoapi-memorystatusex

### Network speed

`GetIfTable2` / `GetIfTable2Ex` can enumerate network interfaces and return interface statistics. Upload/download speed can be calculated by polling byte counters periodically and computing the delta per second.

Sources:

* https://learn.microsoft.com/en-us/windows/win32/api/netioapi/nf-netioapi-getiftable2
* https://learn.microsoft.com/en-us/windows/win32/api/netioapi/nf-netioapi-getiftable2ex

### Desktop app stack

Windows App SDK / WinUI 3 is a good fit for a Windows-only native-feeling utility. Tauri is still possible, but native Windows APIs and always-on-top transparent window behavior may be simpler to control from a Windows-native stack.

Source: https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/

## MVP shape

### Recommended MVP

* Always-on-top compact floating window.
* Shows CPU %, memory %, disk activity %, upload speed, download speed.
* Drag to reposition.
* Settings:
  * opacity
  * refresh interval: 1s / 2s / 5s
  * memory warning threshold
  * start with Windows
* Threshold color states:
  * normal
  * warning
  * critical
* Tray icon:
  * show/hide
  * settings
  * quit

### Good V2 candidates

* Per-metric show/hide.
* Compact and expanded layouts.
* Click-through mode.
* Pin position per monitor.
* Export lightweight usage log.
* GPU usage if feasible through stable Windows APIs.

### Avoid in V1

* CPU/GPU temperature.
* Fan speed.
* Voltage.
* Per-process detailed monitor.
* Full task-manager replacement.
* Cloud sync or accounts.

## Risks

* Always-on-top and transparent window behavior must not interfere with normal desktop use.
* Polling too frequently can make a monitoring tool ironically consume noticeable resources.
* Network speed must handle multiple adapters, virtual adapters, disconnected adapters, VPNs, and counter reset.
* Disk "占用" needs clear definition: disk active time, read/write speed, or used capacity. For a floating monitor, disk active time or read/write throughput is more useful than capacity.

## Recommendation

This direction is viable and probably stronger than some generic productivity tools because it has an instantly understandable value proposition. It is best positioned as:

"一个轻量、不打扰、不需要驱动的 Windows 状态悬浮窗，只显示日常最需要看的系统占用。"

Recommended implementation path:

* V1: Windows-only native app, Windows App SDK / WinUI 3 or WPF.
* Data: Performance Counters + `GlobalMemoryStatusEx` + network interface counters.
* Packaging: MSIX or classic installer depending on distribution preference.
