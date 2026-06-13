---
description: Start the Book Reader AI app, open it in the embedded browser for manual verification of the current changes, then shut everything down. Use when a task requires visual confirmation that UI changes work correctly end-to-end.
---

# Browser Verification — Book Reader AI

You are verifying that the most recent code changes work correctly by running the application and manually testing it in a browser. Follow every step in order. Do not skip cleanup.

## Step 1 — Start the application

Run the app in the background:

```powershell
dotnet run --project BookReaderApp --launch-profile https
```

Use `run_in_background: true`. Capture the process so you can stop it in Step 5.

Wait for the server to be ready. Poll `https://localhost:7009` (or `http://localhost:5235` if HTTPS fails) using WebFetch every 3 seconds until you get a 200 response or a redirect. Time out after 60 seconds and report an error if the server never starts.

Ports come from `BookReaderApp/Properties/launchSettings.json` → `https.applicationUrl`. Recheck that file if the app fails to respond — ports may have changed.

## Step 2 — Open the embedded browser

Open the browser pointed at the app's root URL. Use the `computer_use` browser tool if available in this session. Otherwise open the system default browser with:

```powershell
Start-Process "https://localhost:7009"
```

Confirm the home page loads before continuing.

## Step 3 — Manual test plan

Work through each check below. Navigate, click, and fill in forms as a real user would. Record a pass/fail for each item.

### Core navigation
- [ ] Home page loads without errors
- [ ] Books catalog (`/Books`) shows the list of books
- [ ] Each book card shows the author name (not blank, not an object reference)
- [ ] Book details page (`/Books/Details/{id}`) shows "by <Author Name>"

### Admin / Moderator flows (log in as `admin@bookreader.local` / `Admin#12345`)
- [ ] Create a book — the Author field shows a dropdown of existing authors
- [ ] Selecting an author and submitting creates the book successfully
- [ ] Edit an existing book — the Author dropdown is pre-selected with the correct author
- [ ] Saving the edit redirects to Details and shows the correct author
- [ ] Delete confirmation page shows the author name, not blank

### My Books (log in as `user@bookreader.local` / `User#12345`)
- [ ] My Books page (`/MyBooks`) shows shelf entries with the author name visible

### Edge cases
- [ ] Submitting the Create/Edit form without selecting an author shows a validation error
- [ ] Navigating directly to a non-existent book (`/Books/Details/99999`) returns 404

## Step 4 — Report results

After completing all checks, output a summary table:

| Check | Result | Notes |
|---|---|---|
| Home page loads | ✅/❌ | |
| Books list shows author names | ✅/❌ | |
| ... | | |

If any check fails, describe exactly what went wrong (URL, error message, screenshot description) so the issue can be reproduced and fixed.

## Step 5 — Cleanup

**Always run this, even if earlier steps failed.**

1. Stop the background `dotnet run` process (kill by PID or process name `dotnet`).
2. If you opened the system browser with `Start-Process`, close it:
   ```powershell
   Get-Process msedge, chrome, firefox -ErrorAction SilentlyContinue | Stop-Process -ErrorAction SilentlyContinue
   ```
   Only stop the browser if you launched it in Step 2 and no other tabs were open before you started.
3. Confirm the port is no longer in use:
   ```powershell
   netstat -ano | findstr :7009
   netstat -ano | findstr :5235
   ```

Report "Browser closed and server stopped." when cleanup is complete.
