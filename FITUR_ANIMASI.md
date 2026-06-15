# 🎮 Fitur Animasi - Dokumentasi Lengkap

## ✅ Fitur yang Sudah Ada:

### **1. Control Kecepatan Animasi Lari** ✓

**Parameter di Inspector:**
```
[Animasi]
- Kecepatan Animasi Lari: 1.2 (default)
```

**Cara Kerja:**
- Saat karakter berlari, animasi akan diputar dengan kecepatan yang bisa disesuaikan
- Nilai 1.0 = kecepatan normal
- Nilai 1.2 = 20% lebih cepat (default)
- Nilai 2.0 = 2x lebih cepat
- Nilai 0.5 = setengah kecepatan (slow motion)

**Kode:**
```csharp
animator.speed = bergerak && diTanah ? kecepatanAnimasiLari : 1f;
```

**Efek:**
- Animasi lari akan sync dengan kecepatan movement
- Saat idle/jump, kembali ke speed normal (1.0)
- Smooth transition antar state

---

### **2. Jump Pertama = Animasi Jump, Kedua = Animasi Salto** ✓

**Parameter di Inspector:**
```
[Lompat]
- Double Jump Enabled: ✓ (true)
```

**Cara Kerja:**

#### **Jump Pertama (Spasi di tanah):**
```csharp
if (diTanah)
{
    Lompat(false); // false = bukan salto
}
```
- ✅ Menggunakan parameter animator: `IsJumping = true`
- ✅ Memainkan animasi: **Jump_36f**
- ✅ Set flag: `canDoubleJump = true` (siap untuk double jump)

#### **Jump Kedua (Spasi di udara):**
```csharp
else if (doubleJumpEnabled && canDoubleJump && !isDoubleJump)
{
    Lompat(true); // true = salto!
}
```
- ✅ Trigger animator: `IsSalto`
- ✅ Memainkan animasi: **Salto_Jump_48f**
- ✅ Set flag: `canDoubleJump = false` (tidak bisa jump lagi)

---

## 📋 Parameter Script (Semua di Inspector):

### **[Gerak]**
```
- Kecepatan Lari: 6 (m/s)
- Kecepatan Rotasi: 12 (derajat/s)
- Offset Arah Model: 90 (rotasi model)
```

### **[Lompat]**
```
- Kekuatan Lompat: 8 (kekuatan vertikal)
- Gravitasi: -25 (semakin negatif = jatuh lebih cepat)
- Kecepatan Udara Multiplier: 0.8 (80% speed saat di udara)
- Double Jump Enabled: ✓ (aktifkan double jump)
```

### **[Ground Check]**
```
- Ground Check Point: (Transform, buat empty GameObject child)
- Ground Check Radius: 0.25 (radius deteksi tanah)
- Ground Layer: Ground (layer untuk lantai/platform)
```

### **[Animasi]** ⭐
```
- Kecepatan Animasi Lari: 1.2 (playback speed animasi lari)
```

---

## 🎯 Cara Menggunakan:

### **1. Adjust Kecepatan Animasi Lari:**

**Di Unity Inspector:**
1. Pilih **WUGA_Character**
2. Scroll ke **Gerak Karakter** script
3. Cari section **[Animasi]**
4. Ubah **Kecepatan Animasi Lari**:
   - `0.8` = Lambat (80% speed)
   - `1.0` = Normal
   - `1.2` = Default (sedikit cepat)
   - `1.5` = Fast (50% lebih cepat)
   - `2.0` = Very Fast (2x speed)

**Tips:**
- Sesuaikan dengan `kecepatanLari` agar animasi sync dengan movement
- Jika karakter lari cepat tapi animasi lambat = naikkan nilai
- Jika animasi terlalu cepat sampai kaki blur = turunkan nilai

### **2. Test Jump & Salto:**

**Test Jump Pertama:**
1. Play game
2. Tekan **Spasi** saat di tanah
3. ✅ Harus muncul animasi **Jump** (lompat biasa)

**Test Double Jump:**
1. Saat masih di udara (setelah jump pertama)
2. Tekan **Spasi** lagi
3. ✅ Harus muncul animasi **Salto** (backflip!)

**Disable Double Jump:**
- Uncheck **Double Jump Enabled** di Inspector
- Karakter hanya bisa jump sekali

---

## 🐛 Troubleshooting:

### **"Animasi lari terlalu cepat/lambat"**
**Solusi:**
- Adjust `Kecepatan Animasi Lari` di Inspector
- Recommended range: 0.8 - 1.5

### **"Jump kedua tidak muncul animasi salto"**
**Cek:**
1. ✅ `Double Jump Enabled` = checked
2. ✅ Animator Controller punya parameter `IsSalto` (trigger)
3. ✅ Animator punya state "Salto" dengan animasi `Salto_Jump_48f`
4. ✅ Ada transition dari Any State → Salto (kondisi: IsSalto trigger)

### **"Jump pertama langsung salto"**
**Penyebab:**
- Animator transition salah
- Pastikan jump pertama pakai parameter `IsJumping` (bool), bukan `IsSalto`

### **"Animasi tidak smooth"**
**Solusi:**
- Cek Animator transition duration (0.15 - 0.25 recommended)
- Pastikan "Has Exit Time" di-check untuk state Jump/Salto

---

## 🎨 Animator Controller Setup:

### **States:**
```
1. Idle (Idle_Loop_48f)
2. Run (Run_Cycle_Reference_24f)
3. Jump (Jump_36f) ← Jump pertama
4. Salto (Salto_Jump_48f) ← Jump kedua
```

### **Parameters:**
```
- Speed (Float): 0 = idle, 1 = run
- IsJumping (Bool): true = sedang jump
- IsSalto (Trigger): trigger salto animation
```

### **Transitions:**
```
Idle ↔ Run:
  - Idle → Run: Speed > 0.1
  - Run → Idle: Speed < 0.1

Any State → Jump:
  - Condition: IsJumping = true
  - Jump → Idle: IsJumping = false, HasExitTime = true

Any State → Salto:
  - Condition: IsSalto (trigger)
  - Salto → Idle: HasExitTime = true
```

---

## 📊 Contoh Nilai Recommended:

### **Untuk gameplay cepat (action):**
```
Kecepatan Lari: 8
Kecepatan Animasi Lari: 1.5
Kekuatan Lompat: 10
Gravitasi: -30
```

### **Untuk gameplay normal (balanced):**
```
Kecepatan Lari: 6
Kecepatan Animasi Lari: 1.2
Kekuatan Lompat: 8
Gravitasi: -25
```

### **Untuk gameplay lambat (cinematic):**
```
Kecepatan Lari: 4
Kecepatan Animasi Lari: 1.0
Kekuatan Lompat: 6
Gravitasi: -20
```

---

## ✨ Summary:

✅ **Kecepatan Animasi Lari**: Sudah ada! Parameter `kecepatanAnimasiLari` di Inspector
✅ **Jump Pertama = Jump**: Menggunakan parameter `IsJumping` dan animasi `Jump_36f`
✅ **Jump Kedua = Salto**: Menggunakan trigger `IsSalto` dan animasi `Salto_Jump_48f`

**Tinggal tune parameter di Inspector sesuai keinginan!** 🎮
