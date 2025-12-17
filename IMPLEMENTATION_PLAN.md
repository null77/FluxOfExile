# FluxOfExile - Implementation Plan

## Overview
A Windows application that helps manage Path of Exile (1 & 2) gaming time by:
- Tracking daily playtime
- Gradually dimming the game window as the time limit approaches
- Presenting alerts near and past the deadline
- Resetting the counter at a configurable time each day

**Supported Games:**
- Path of Exile
- Path of Exile 2

---

## Phase 0: Technical Proof of Concept (Window Dimming)

**Goal:** Validate that we can reliably dim a specific window on Windows, and discover the correct window title patterns for PoE 1 & 2.

### Technical Approach Options

1. **Overlay Window Approach** (Recommended)
   - Create a transparent, click-through overlay window positioned over the target
   - Adjust overlay opacity to create dimming effect
   - Pros: Non-invasive, works with any window, doesn't modify game
   - Cons: May be affected by fullscreen exclusive mode

2. **Desktop Window Manager (DWM) Approach**
   - Use Windows DWM APIs to modify window appearance
   - More integrated but more complex

3. **Color/Gamma Adjustment**
   - Modify display gamma for the specific monitor
   - Affects entire display, not ideal

### Technical Test Implementation

```
Project: FluxOfExile.TechTest
Type: Windows Forms Application (.NET 8)
```

**Test Features:**
1. **Window Discovery** - Enumerate all windows, identify PoE 1 & 2 window titles/classes
2. Find Path of Exile windows by discovered patterns
3. Create overlay window that tracks target window position/size
4. Debug controls to manually set dimming levels (0%, 25%, 50%, 75%, 90%)
5. Handle window move/resize/minimize/restore events
6. Test with windowed, borderless windowed, and fullscreen modes

**Success Criteria:**
- [ ] Successfully identify PoE 1 and PoE 2 windows
- [ ] Overlay correctly tracks window position and size
- [ ] Dimming is visually effective at various levels
- [ ] Click-through works (game remains playable)
- [ ] Works in borderless windowed mode (primary target)
- [ ] Graceful handling when game window closes/minimizes

---

## Phase 1: Core Application Framework

**Goal:** Build the basic application structure with settings persistence.

### Components

1. **System Tray Application**
   - Minimize to system tray
   - Right-click context menu
   - Tray icon shows status (normal, warning, overtime)

2. **Settings Model**
   ```
   - DailyTimeLimitMinutes: int (default: 120)
   - ResetTimeOfDay: TimeOnly (default: 04:00 AM)
   - WarningThresholdMinutes: int (default: 15)
   - DimStartPercent: int (default: 10)
   - DimEndPercent: int (default: 80)
   - OvertimeAlertIntervalMinutes: int (default: 15)
   ```

3. **Settings Storage**
   - JSON file in AppData/Local/FluxOfExile
   - Auto-save on changes

4. **Simple Settings UI**
   - Basic form to configure all settings
   - Input validation

---

## Phase 2: Time Tracking

**Goal:** Accurately track time spent in Path of Exile (1 or 2).

### Design Decisions
- **Focus-only tracking:** Time only counts when a PoE window is focused (not just running)
- **Combined tracking:** PoE 1 and PoE 2 time counts toward the same daily limit
- **Pause feature:** User can manually pause the timer for breaks

### Components

1. **Process Monitor**
   - Detect when PoE 1 or PoE 2 window is focused
   - Poll every 1-5 seconds
   - Track which game is currently active (for display purposes)

2. **Session Tracker**
   - Track current session duration
   - Persist accumulated time to file (survive app restart)
   - Handle day rollover at reset time
   - Support manual pause/resume

3. **State Model**
   ```
   - TodayAccumulatedMinutes: int
   - CurrentSessionStartTime: DateTime?
   - LastResetDate: DateOnly
   - IsPaused: bool
   ```

---

## Phase 3: Dimming Integration

**Goal:** Apply progressive dimming based on time remaining.

### Dimming States

| State | Time Remaining | Dim Level | Visual |
|-------|---------------|-----------|--------|
| Normal | > WarningThreshold | 0% | Clear |
| Warning | 0 to WarningThreshold | 0% to DimStart | Slight dim |
| Critical | At limit | DimEnd | Heavy dim |
| Overtime | Past limit | DimEnd | Heavy dim (persistent until daily reset) |

### Design Decisions
- **Overtime behavior:** Max dim stays indefinitely until the daily reset time
- **Reset clears everything:** At reset time, accumulated time goes to zero and dimming is removed

### Implementation
- Linear interpolation between dim levels during warning phase
- Smooth transitions (animate over 1-2 seconds)
- Only apply when PoE window exists

---

## Phase 4: Alert System

**Goal:** Notify user of time status.

### Alert Types

1. **Warning Alert** (approaching limit)
   - Toast notification at warning threshold
   - "15 minutes remaining in your session"

2. **Limit Reached Alert**
   - More prominent notification
   - "You've reached your daily limit"

3. **Overtime Alerts**
   - Repeat every 15 minutes (configurable)
   - "You are X minutes over your limit"

### Implementation
- Windows Toast Notifications
- Optional: sound alerts
- Respect Windows "Focus Assist" / "Do Not Disturb"?

---

## Phase 5: Debug & Testing Features

**Goal:** Tools to verify functionality without waiting.

### Debug Panel (Development Build)
- Manual time injection (set current accumulated time)
- Force dimming level slider
- Trigger each alert type manually
- Reset daily counter
- Show real-time state (time tracked, dim level, etc.)

### Debug Hotkeys
- Configurable keyboard shortcuts for testing
- Only enabled in debug mode

---

## Phase 6: Polish & Edge Cases

**Goal:** Handle real-world usage scenarios.

### Edge Cases
- Computer sleep/hibernate handling
- Multiple PoE instances (PoE 1 and PoE 2 running simultaneously)
- PoE crash/force close
- App crash recovery (persist state frequently)
- Windows user session lock/unlock
- Daylight saving time transitions

### Polish
- First-run wizard
- Graceful shutdown
- Startup with Windows (optional setting)
- Update checking (future consideration)

---

## Technology Stack

- **Framework:** .NET 8.0
- **UI:** Windows Forms (simple, native, low overhead)
- **Packaging:** Single-file executable
- **Minimum OS:** Windows 10 (for modern toast notifications)

---

## File Structure (Proposed)

```
FluxOfExile/
├── src/
│   ├── FluxOfExile/              # Main application
│   │   ├── Program.cs
│   │   ├── MainForm.cs           # Tray app main form
│   │   ├── SettingsForm.cs
│   │   ├── DebugForm.cs
│   │   ├── Services/
│   │   │   ├── ProcessMonitor.cs
│   │   │   ├── TimeTracker.cs
│   │   │   ├── OverlayManager.cs
│   │   │   └── AlertService.cs
│   │   ├── Models/
│   │   │   ├── Settings.cs
│   │   │   └── SessionState.cs
│   │   └── Forms/
│   │       └── OverlayForm.cs
│   │
│   └── FluxOfExile.TechTest/     # Technical test project
│       ├── Program.cs
│       ├── TestForm.cs
│       └── OverlayForm.cs
│
├── FluxOfExile.sln
└── IMPLEMENTATION_PLAN.md
```

---

## Immediate Next Steps

1. **Create solution and tech test project**
2. **Implement window enumeration to discover PoE 1 & 2 window titles**
3. **Implement basic overlay window**
4. **Add window tracking (find PoE, follow position)**
5. **Add debug controls for dimming levels**
6. **Test with Path of Exile 1 & 2 in various window modes**

---

## Risk Assessment

| Risk | Mitigation |
|------|------------|
| Fullscreen exclusive mode blocks overlay | Document limitation; recommend borderless windowed |
| Anti-cheat flags overlay as suspicious | Overlay is purely visual, doesn't interact with game memory |
| Performance impact | Minimal - overlay is static, only updates on window move |
| PoE updates change window class/name | Use flexible pattern matching; discovery tool helps identify |

---

## Design Decisions (Resolved)

1. **Focus-only tracking:** Time only counts when PoE is focused, not just running
2. **Pause feature:** Yes - user can pause timer for breaks
3. **Overtime dimming:** Max dim stays indefinitely until daily reset time
4. **Combined tracking:** PoE 1 and PoE 2 share the same daily time limit
