# Mobile QR Scan Testing Guide

## 🎯 Quick Diagnosis: Why QR Scan Fails

### The Issue
When you scan a QR code with your **phone**, you can't access the batch information. This happens because the frontend is still trying to use `localhost:5085` instead of the actual server IP.

### Root Cause
```
Phone opens: http://192.168.1.10:5173 (frontend via LAN ✓)
Frontend tries to call: http://localhost:5085 (WRONG - means phone's own localhost ✗)
Result: API fails, batch never created, QR scan shows empty data ✗
```

---

## ✅ Step 1: Update Frontend Configuration

### Edit `.env` file
**File**: `BlueFood_frontend\.env`

**Find:**
```
VITE_API_BASE_URL=http://localhost:5085
```

**Replace with YOUR LAN IP** (change `192.168.1.10` to your actual IP):
```
VITE_API_BASE_URL=http://192.168.1.10:5085
```

### How to find your LAN IP
**On Windows, run this command:**
```powershell
ipconfig
```
Look for "IPv4 Address" under your network adapter (usually starts with 192.168.x.x or 10.x.x.x)

---

## ✅ Step 2: Verify Backend Connection String

### Check Database is Accessible
**File**: `BlueFood_Api/appsettings.Development.json`

Should look like:
```json
{
  "ConnectionStrings": {
    "BlueFoodDb": "Server=BAKHANG\\SQLEXPRESS;Database=BlueFoodSCM;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True;MultipleActivityResultSets=True"
  }
}
```

✅ This is correct for local Windows authentication. No changes needed.

---

## ✅ Step 3: Start Both Services

### Terminal 1: Start Backend (Database Connected)
```powershell
cd D:\ky2_2526\qlda\BlueFood_backend
$env:ASPNETCORE_ENVIRONMENT='Development'
dotnet run --project BlueFood_Api/BlueFood.Api.csproj --urls http://0.0.0.0:5085
```

**Expected output:**
```
Application listening on: http://0.0.0.0:5085
info: Microsoft.AspNetCore.Hosting.Diagnostics[1]
      Request starting HTTP/1.1 GET http://localhost:5085/swagger - -
```

### Terminal 2: Start Frontend (Access from any machine)
```powershell
cd D:\ky2_2526\qlda\BlueFood_frontend
npm run dev -- --host 0.0.0.0 --port 5173
```

**Expected output:**
```
VITE v7.1.5  ready in 123 ms

➜  Local:   http://localhost:5173/
➜  Network:  http://192.168.1.10:5173/
```

---

## ✅ Step 4: Test Desktop Browser First

1. **Open desktop browser**: `http://localhost:5173`
2. **Create a batch**:
   - Mã lô: `BF-2026-001`
   - Tên sản phẩm: `Xoai Cat Chu`
   - Click `Tạo batch + QR`
3. **Verify**:
   - You see a QR code displayed
   - Copy the "Trace URL" shown (should be `http://{YOUR_IP}:5085/t/...`)

---

## ✅ Step 5: Test on Phone (Same Network)

### Prerequisites
- Phone must be on the **same WiFi network** as your computer
- Backend and frontend must both be running

### Test Steps

1. **On your phone, open browser** and navigate to:
   ```
   http://192.168.1.10:5173
   ```
   (Replace `192.168.1.10` with your actual LAN IP)

2. **Verify frontend loads**:
   - You should see the BlueFood dashboard
   - NOT a blank page or error

3. **Create a batch on phone**:
   - Enter batch code, product name
   - Click `Tạo batch + QR`
   - **This is the critical test** - if API calls work, you'll see the QR code
   - If this fails, check browser console (F12) for network errors

4. **Scan the QR code** with your phone's camera:
   - Point camera at the QR code displayed on screen
   - Tap the notification to open the link
   - You should see batch details on the public trace page

---

## 🔍 Troubleshooting

### ❌ Problem: "Can't reach http://192.168.1.10:5173 on phone"
**Solution:**
- Check phone and computer are on the same WiFi
- Check Windows Firewall allows port 5173 and 5085
- Try pinging: `ping 192.168.1.10` from phone if it supports it

### ❌ Problem: "Frontend loads but API calls fail (shows error in status)"
**Solution:**
- Check `.env` file has correct IP: `VITE_API_BASE_URL=http://192.168.1.10:5085`
- Check backend is running: `http://192.168.1.10:5085/swagger` should load
- Hard refresh browser: `Ctrl+Shift+R` (or on phone: pull refresh)

### ❌ Problem: "Backend returns 500 error on QR scan"
**Solution:**
- Check SQL Server is running
- Check connection string in `appsettings.Development.json`
- Look at backend console for error details
- Try creating batch from desktop first

### ❌ Problem: "QR code but public trace page is empty"
**Solution:**
- Batch was created successfully (QR works)
- But phone can't query the database
- This usually means the phone couldn't reach the backend at QR creation time
- Solution: Go back to "create batch" step - ensure API calls worked

---

## 📊 Complete Testing Checklist

- [ ] Found your actual LAN IP with `ipconfig`
- [ ] Updated `BlueFood_frontend\.env` with the IP
- [ ] Backend running: `http://192.168.1.10:5085/swagger` loads on desktop
- [ ] Frontend running: `http://localhost:5173` loads on desktop
- [ ] Desktop: Can create batch and see QR code
- [ ] Desktop: QR code copy shows correct URL with IP (not localhost)
- [ ] Phone: Can open `http://192.168.1.10:5173` from phone
- [ ] Phone: Can create batch and see QR code
- [ ] Phone: Can scan QR code that opens public trace page
- [ ] Phone: Public trace page shows batch details

---

## 🎓 How It Works (Technical Overview)

```
┌─────────────────────────────────────────────────────────────┐
│ Desktop (localhost:5173)     Phone (192.168.1.10:5173)      │
│ ↓                             ↓                              │
│ Frontend API Client           Frontend API Client            │
│ ↓                             ↓                              │
│ VITE_API_BASE_URL=            VITE_API_BASE_URL=            │
│ http://localhost:5085 (OK)    http://192.168.1.10:5085 ✓   │
│         ↓                             ↓                     │
│         └─────────────────────────────┘                     │
│                      ↓                                       │
│              Backend (5085)                                  │
│         Database Connection:                                │
│         Server=BAKHANG\SQLEXPRESS ✓                         │
│                      ↓                                       │
│              SQL Server Database                            │
│                      ↓                                       │
│            Batch Data Retrieved ✓                           │
└─────────────────────────────────────────────────────────────┘

QR Code contains:
http://192.168.1.10:5085/t/{qrToken}

When phone scans and accesses:
✓ Phone GET /t/{qrToken}
✓ Backend retrieves batch data from DB
✓ Renders HTML with batch info
✓ Phone displays public trace page
```

---

## 📝 Notes for Demo

- **Desktop users**: Can always use `http://localhost:5173`
- **Phone users**: Must use LAN IP from the `.env` file
- **After changing `.env`**: Restart frontend with `npm run dev`
- **Database**: Remains unchanged (uses Windows Auth on BAKHANG machine)
- **QR Codes**: Now point to correct IP so phones can access

Good luck! 🚀
