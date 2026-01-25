# Industrial HUD Setup Guide

## What Was Created

A complete industrial/safety-themed HUD using Unity UI Toolkit with the following features:

### Visual Theme
- **Safety Yellow** labels (like warning signs)
- **Hazard Orange** enemy counter
- **Industrial Grey** panels with semi-transparent backgrounds
- **Green** health bar (turns red at low health)
- **Blue** XP bar
- Clean, professional "Company Safety Manual" aesthetic

### HUD Elements
1. **Top Left Panel**: Health bar + Level display
2. **Top Right Panel**: Room counter (Sector X/25) + Enemy counter (Hostiles)
3. **Bottom Center**: XP bar (Performance Review)
4. **Center Screen**: Notification system (for "ROOM CLEARED", "LEVEL UP", etc.)

## Setup Instructions (In Unity Editor)

### Step 1: Import Files
The following files were created:
- `Assets/_Project/UI/GameplayHUD.uxml` - UI layout
- `Assets/_Project/UI/GameplayHUD.uss` - Stylesheet
- `Assets/_Project/Scripts/UI/GameplayHUDController.cs` - Controller script

### Step 2: Link Stylesheet to UXML
1. Open `GameplayHUD.uxml` in Unity
2. In the UI Builder (or text editor), add this line after the opening `<ui:UXML>` tag:
   ```xml
   <Style src="GameplayHUD.uss" />
   ```
   Or drag the `.uss` file into the StyleSheets section of the UI Builder.

### Step 3: Create HUD GameObject in TestScene
1. Open `TestScene.unity`
2. Create new GameObject: `GameObject → UI Toolkit → UI Document`
3. Rename it to "GameplayHUD"
4. In the Inspector:
   - **UIDocument Component**:
     - Source Asset: Drag `GameplayHUD.uxml` here
   - **Add Component**: `GameplayHUDController`
   - The controller should auto-reference the UIDocument

### Step 4: Verify Event Connections
The HUD automatically subscribes to these GameEvents:
- `OnHealthChanged` - Already wired in PlayerController ✅
- `OnXPChanged` - Already fired by XPManager ✅
- `OnLevelUp` - Already fired by XPManager ✅
- `OnRoomEntered` - Needs RoomManager implementation
- `OnRoomCleared` - Needs RoomManager implementation
- `OnEnemyKilled` - Already fired ✅

### Step 5: Update WaveSpawner (Optional - Enemy Counter)
To show enemy count, add this to `WaveSpawner.cs`:

```csharp
void Start() {
    // ... existing code ...
    
    // Update HUD with initial enemy count
    GameplayHUDController hud = FindObjectOfType<GameplayHUDController>();
    if (hud != null) {
        hud.SetEnemyCount(GetTotalEnemyCount());
    }
}
```

## Testing Checklist

### In Play Mode:
1. **Health Bar**: Take damage → bar should decrease and show current/max HP
2. **XP Bar**: Kill enemies → bar should fill up
3. **Level Display**: Gain enough XP → level number should increase
4. **Notifications**: Level up → "CLEARANCE LEVEL X" notification should appear
5. **Low Health**: Get below 30% HP → health bar turns red and glows

## Color Customization

To adjust colors, edit `GameplayHUD.uss` variables at the top:

```css
:root {
    --safety-yellow: rgb(255, 215, 0);      /* Labels */
    --hazard-orange: rgb(255, 120, 0);      /* Enemy counter */
    --health-color: rgb(80, 220, 120);      /* Health bar */
    --xp-color: rgb(100, 180, 255);         /* XP bar */
    /* ... etc ... */
}
```

## Troubleshooting

### "UI elements not found" error
- Make sure the UXML file is properly assigned to the UIDocument component
- Verify the stylesheet is linked in the UXML file

### UI not showing
- Check that Panel Settings are configured in Project Settings → UI Toolkit
- Make sure the Canvas has a Sort Order that renders above the game view

### Events not updating UI
- Check Console for event subscription messages
- Verify GameEvents are being fired (add Debug.Log in event handlers)

## Next Steps (Phase 2)

Once the HUD is working:
1. Implement RoomManager to fire OnRoomEntered/OnRoomCleared
2. Create UpgradeUI using the same industrial style
3. Add room modifiers/hazards display
4. Add boss health bar (Phase 2)
5. Add combo/kill streak counter (Phase 3)

## Architecture Notes

- **UI Toolkit** chosen over Canvas for performance and scalability
- **Event-driven** - HUD never directly references game objects
- **Self-contained** - Can be dropped into any scene
- **Theme-consistent** - All future UI should use the same USS variables
