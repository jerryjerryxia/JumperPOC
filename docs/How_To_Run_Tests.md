# How to Run Tests in Unity - Step-by-Step Guide

## Step 1: Open Unity Project

1. **Open Unity Hub**
2. **Click on "JumperPOC"** to open the project
3. **Wait for Unity to finish importing** (you'll see a progress bar at the bottom)

---

## Step 2: Force Reimport Test Files (If Needed)

If Unity hasn't detected the new test files yet:

1. In Unity Editor, go to **Assets** menu
2. Click **Refresh** (or press `Ctrl+R`)
3. Wait for Unity to reimport everything

**Alternative Method:**
1. Right-click on `Assets/Tests` folder in Project window
2. Select **Reimport**

---

## Step 3: Open Test Runner Window

### Method 1: Via Menu
1. In Unity Editor, click **Window** menu (top menu bar)
2. Hover over **General**
3. Click **Test Runner**

```
Window → General → Test Runner
```

### Method 2: Via Shortcut
- Press `Ctrl+Alt+T` (Windows)
- Press `Cmd+Alt+T` (Mac)

### What You Should See:
A new window titled **Test Runner** will appear. It has two tabs:
- **EditMode** (this is what we want)
- **PlayMode** (empty for now)

---

## Step 4: Navigate to EditMode Tests

1. In the **Test Runner** window, click the **EditMode** tab (if not already selected)
2. You should see a folder tree structure like this:

```
📁 Tests.EditMode
  📁 Tests.EditMode
    📄 PlayerAbilitiesTests (19 tests)
    📄 PlayerStateTrackerTests (26 tests)
    📄 PlayerInputHandlerTests (16 tests)
```

### If You Don't See Any Tests:

**Problem:** Unity hasn't compiled the test assemblies yet.

**Solution:**
1. Close Test Runner window
2. Go to **Assets** → **Refresh** (or `Ctrl+R`)
3. Wait 10-20 seconds for compilation
4. Reopen Test Runner: **Window** → **General** → **Test Runner**
5. Click **EditMode** tab again

**Still Nothing?**
- Check Unity Console (Window → General → Console) for compilation errors
- Look for red error messages about the test files
- Share those errors with me and I'll help fix them

---

## Step 5: Run All Tests

### Option A: Run All Tests at Once (Recommended First Time)

1. Make sure **EditMode** tab is selected
2. At the top of the Test Runner window, click **"Run All"** button
3. Watch the tests execute (should take <5 seconds)

### What You Should See During Test Run:
- Progress bar showing test execution
- Test names appearing with icons:
  - ✅ **Green checkmark** = Test passed
  - ❌ **Red X** = Test failed
  - ⏸ **Gray circle** = Test not run yet

### Expected Result:
```
✅ 61 tests passed
❌ 0 tests failed
Total time: ~3-5 seconds
```

---

## Step 6: Understanding Test Results

### All Green (Success) ✅
If all 61 tests show green checkmarks:
- **Congratulations!** All tests are passing
- Your code is working correctly
- You can safely refactor with confidence

### Some Red (Failures) ❌
If any tests show red X marks:

1. **Click on the failed test** to see error details
2. **Read the error message** in the bottom panel
3. **Common errors and fixes:**

#### Error: "Type or namespace 'NUnit' could not be found"
**Cause:** Assembly definition issue
**Fix:**
1. Right-click `Assets/Tests/EditMode/Tests.EditMode.asmdef`
2. Select **Reimport**
3. Wait for recompilation
4. Run tests again

#### Error: "PlayerAbilities does not exist"
**Cause:** Test assembly can't reference game code
**Fix:**
1. Open `Assets/Tests/EditMode/Tests.EditMode.asmdef` in Unity
2. Check that "Assembly-CSharp" is in the references list
3. If missing, add it
4. Apply and run tests again

#### Error: "Object reference not set to an instance"
**Cause:** Missing component initialization
**Fix:**
1. Click the failed test to see which component
2. Share the full error message with me
3. I'll help debug the specific issue

---

## Step 7: Run Individual Tests

To test one specific test:

1. In Test Runner, **expand the test file** (click the arrow next to it)
2. **Expand the test class** (e.g., PlayerAbilitiesTests)
3. **Click on a specific test** (e.g., "SetAbility_UnlocksDoubleJump_WhenCalled")
4. Click **"Run Selected"** button at the top

### Use Cases:
- **Debugging specific behavior**
- **Verifying a bug fix**
- **Understanding what a test does**

---

## Step 8: Interpreting Test Names

Our tests follow this naming pattern:
```
MethodName_ExpectedBehavior_WhenCondition()
```

### Examples:

| Test Name | What It Tests |
|-----------|---------------|
| `SetAbility_UnlocksDoubleJump_WhenCalled` | SetAbility() method unlocks double jump when called |
| `UpdateStates_SetsIsRunning_WhenGroundedAndMoving` | UpdateStates() sets IsRunning to true when player is grounded and moving |
| `OnMove_InvokesEvent_WithCorrectVector` | OnMove event fires with the correct input vector |

---

## Troubleshooting Guide

### Problem: Test Runner window is blank/empty

**Possible Causes:**
1. Unity hasn't imported test files yet
2. Compilation errors preventing test assembly build
3. Assembly definitions not configured correctly

**Solutions:**
1. **Check Console for errors:**
   - Window → General → Console
   - Look for red errors about test files
   - Fix any compilation errors first

2. **Reimport test assembly:**
   - Right-click `Assets/Tests` folder
   - Select **Reimport**
   - Wait for Unity to recompile

3. **Restart Unity:**
   - Save project
   - Close Unity
   - Reopen project
   - Open Test Runner again

---

### Problem: Tests compile but don't appear in Test Runner

**Solution:**
1. Click the **"⋮" (three dots)** menu in Test Runner
2. Select **"Recompile and run tests"**
3. Wait for compilation to finish

---

### Problem: Tests were passing, now they fail

**Common Causes:**
1. **Code changes broke functionality** - This is GOOD! Tests caught a regression
2. **Inspector values changed** - Check if serialized fields in prefabs changed
3. **Unity version mismatch** - Verify Unity version matches project

**What to do:**
1. Read the failure message carefully
2. Check what code changed recently (`git diff`)
3. Revert recent changes to see if tests pass again
4. Fix the code that broke the test

---

## Visual Guide (Text Description)

Since I can't show actual screenshots, here's what to look for:

### Unity Editor Layout:
```
+--------------------------------------------------+
| File  Edit  Assets  GameObject  Component  Window|
+--------------------------------------------------+
|                                                   |
|  [Project Window]      [Scene View]              |
|  Assets/               [Your game scene]         |
|    └─ Tests/                                     |
|        ├─ EditMode/    [Hierarchy]               |
|        │   ├─ Tests.EditMode.asmdef             |
|        │   ├─ PlayerAbilitiesTests.cs            |
|        │   ├─ PlayerStateTrackerTests.cs         |
|        │   └─ PlayerInputHandlerTests.cs         |
|        └─ TestHelpers/                           |
|                                                   |
+--------------------------------------------------+
| [Console Window - Check for errors here]         |
+--------------------------------------------------+
```

### Test Runner Window:
```
+----------------------------------+
| Test Runner               [X]    |
+----------------------------------+
| [EditMode] [PlayMode]            |
| [Run All] [Run Selected] [Rerun] |
+----------------------------------+
| Search: [____________]  🔍       |
+----------------------------------+
| 📁 Tests.EditMode                |
|   📁 Tests.EditMode              |
|     📄 PlayerAbilitiesTests      |
|       ✅ SetAbility_UnlocksDoubleJump...
|       ✅ SetAbility_LocksDash...
|       ... (17 more tests)
|     📄 PlayerStateTrackerTests   |
|       ✅ UpdateStates_SetsIsRunning...
|       ... (25 more tests)
|     📄 PlayerInputHandlerTests   |
|       ✅ OnMove_InvokesEvent...
|       ... (15 more tests)
+----------------------------------+
| Test Details:                    |
| [Selected test info appears here]|
+----------------------------------+
```

---

## Quick Reference Commands

| Action | Steps |
|--------|-------|
| **Open Test Runner** | Window → General → Test Runner |
| **Refresh Tests** | Assets → Refresh (Ctrl+R) |
| **Run All Tests** | Test Runner → Run All |
| **Run One Test** | Click test → Run Selected |
| **See Error Details** | Click failed test → Check bottom panel |
| **Recompile Tests** | Test Runner menu (⋮) → Recompile |

---

## What to Do After Running Tests

### If All Tests Pass (✅ 61/61):

1. **Celebrate!** 🎉 You have working unit tests
2. **Commit to Git:**
   ```bash
   git add .
   git commit -m "Add Phase 1 unit tests - 61 tests passing"
   git push
   ```
3. **Continue development** with confidence

### If Some Tests Fail (❌):

1. **Don't panic** - This is normal for first run
2. **Read the error messages** carefully
3. **Share the errors** - Send me:
   - Which test failed
   - The full error message
   - Any stack trace shown
4. **I'll help fix it** - Most are quick fixes

---

## Next Steps After Tests Pass

1. **Run tests before refactoring** - Ensure current behavior works
2. **Run tests after refactoring** - Verify nothing broke
3. **Add new tests** when fixing bugs or adding features
4. **Keep tests green** - Never commit if tests are failing

---

## Need Help?

If tests aren't showing up or you're stuck:

1. **Check Console first** (Window → General → Console)
2. **Take a screenshot** of the Test Runner window
3. **Share any error messages** from Console
4. **Tell me what step you're on**

I'll help you debug!

---

**Last Updated:** 2025-10-20
**Expected Test Count:** 61 tests
**Expected Pass Rate:** 100% ✅
