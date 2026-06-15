# 🎮 Setup Sistem Fisika - Panduan Lengkap (FIXED)

## ⚠️ PERBAIKAN BUG TENGGELAM

### **Masalah yang Diperbaiki:**
1. ✅ Ground detection menggunakan **SphereCast** (lebih reliable)
2. ✅ Velocity vertikal di-snap ke nilai kecil saat di tanah (tidak tenggelam)
3. ✅ Ground check dipindah ke `FixedUpdate()` untuk sinkron dengan physics
4. ✅ Menggunakan `col.bounds` untuk posisi yang akurat
5. ✅ Menambahkan visual debug (Gizmos) untuk troubleshooting

---

## 🔧 Setup di Unity Editor (WAJIB!)

### **A. Setup Karakter (WUGA_Character)**

1. **Pilih GameObject karakter** di Hierarchy

2. **Tambahkan/Verifikasi Komponen (akan otomatis):**
   - ✅ Animator
   - ✅ GerakKarakter script
   - ⚠️ **Rigidbody** (ditambahkan otomatis)
   - ⚠️ **CapsuleCollider** (ditambahkan otomatis)

3. **PENTING - Setting Rigidbody:**
   ```
   Mass: 1
   Drag: 0
   Angular Drag: 0.05
   
   ⚠️ Use Gravity: ✗ UNCHECK (PENTING!)
   ⚠️ Is Kinematic: ✗ UNCHECK
   
   Interpolate: Interpolate
   Collision Detection: Continuous Dynamic
   
   Constraints - Freeze Rotation:
     ✓ X (checked)
     ✗ Y (unchecked - biar bisa putar)
     ✓ Z (checked)
   ```

4. **Setting CapsuleCollider:**
   ```
   ⚠️ SESUAIKAN DENGAN MODEL ANDA!
   
   Material: None (Physics Material)
   Is Trigger: ✗ UNCHECK
   
   Center: 
     X: 0
     Y: 1.0 (setengah tinggi model)
     Z: 0
   
   Radius: 0.3 (lebar model / 2)
   Height: 2.0 (tinggi model)
   Direction: Y-Axis
   ```
   
   **Cara mengetahui nilai yang tepat:**
   - Tinggi model anda (misal 2 meter) → Height = 2
   - Center Y = Height / 2 = 1
   - Lebar model (misal 0.6 meter) → Radius = 0.3

5. **Setting GerakKarakter Script:**
   ```
   [Kecepatan]
   - Kecepatan Lari: 6
   - Kecepatan Rotasi: 12
   
   [Fisika]
   - Gravitasi: -20
   - Kekuatan Lompat: 8
   
   [Ground Detection]
   - Ground Check Radius: 0.2
   - Ground Check Distance: 0.3
   - Velocity Snap Threshold: 0.5
   ⚠️ Ground Layer: Everything (SEMENTARA - ubah nanti)
   
   [Animasi]
   - Kecepatan Animasi Lari: 1.2
   
   [Double Jump]
   ✓ Double Jump Enabled
   - Double Jump Min Delay: 0.2
   
   [Lompatan]
   - Kecepatan Udara Multiplier: 0.8
   
   [Arah Model]
   - Offset Arah Model: 90
   ```

---

### **B. Setup Lantai/Platform/Tembok**

#### **Langkah 1: Buat Layer "Ground"**

1. **Edit → Project Settings → Tags and Layers**
2. **User Layer 6**, ketik: `Ground`
3. **Close Project Settings**

#### **Langkah 2: Setup Semua Platform/Lantai**

**Untuk setiap GameObject lantai/platform/dinding:**

1. **Pilih GameObject** di Hierarchy
2. **Inspector → Layer → Ground**
3. **Tambahkan Collider** jika belum ada:
   - Add Component → **Box Collider** (untuk kubus/kotak)
   - ATAU **Mesh Collider** (untuk mesh kompleks)
4. **Setting Collider:**
   ```
   ⚠️ Is Trigger: ✗ UNCHECK (HARUS!)
   Convex: ✗ (untuk Mesh Collider)
   ```
5. **Tambahkan Script:**
   - Add Component → Scripts → **ObjekTembok**

#### **Langkah 3: Update Karakter Ground Layer**

1. **Pilih karakter** di Hierarchy
2. **GerakKarakter script → Ground Layer**
3. **Uncheck semua layer KECUALI "Ground"**
4. **Hanya centang: Ground** ✓

---

### **C. Setup Collision Matrix (Recommended)**

1. **Edit → Project Settings → Physics**
2. **Scroll ke bawah ke "Layer Collision Matrix"**
3. **Pastikan "Default" bisa collide dengan "Ground"** (ada checkmark)

---

## 🐛 Debug & Testing

### **Visual Debug (Gizmos):**

Saat karakter dipilih di Hierarchy, akan muncul:
- **Sphere hijau** = Di tanah ✓
- **Sphere merah** = Di udara
- **Garis ke bawah** = Arah deteksi tanah

### **Test Checklist:**

1. ✅ **Karakter berdiri di lantai (tidak tenggelam)**
   - Sphere debug berwarna hijau
   - Posisi stabil, tidak jittering
   
2. ✅ **Gravitasi berfungsi**
   - Jika diangkat ke atas, jatuh ke bawah
   - Tidak melayang
   
3. ✅ **Collision dengan tembok**
   - Tidak bisa menembus
   - Tidak meleset/glitch
   
4. ✅ **Jump & Landing**
   - Spasi = jump smooth
   - Mendarat = sphere jadi hijau
   - Double jump = salto di udara
   
5. ✅ **Movement di platform**
   - Bisa jalan di atas platform
   - Tidak jatuh dari platform saat jalan

---

## ❌ Troubleshooting

### **1. Karakter MASIH Tenggelam**

**Penyebab:**
- Collider terlalu besar/kecil
- Ground Layer tidak diset

**Solusi:**
```
A. Cek CapsuleCollider:
   - Center Y = tinggi model / 2
   - Height = tinggi model
   - Radius = lebar model / 2

B. Cek GerakKarakter:
   - Ground Layer = HANYA Ground (tidak Everything)
   
C. Cek platform:
   - Layer = Ground
   - Collider Is Trigger = UNCHECK
```

### **2. Karakter Melayang di Atas Lantai**

**Penyebab:**
- Ground Check Distance terlalu kecil
- Collider height/center salah

**Solusi:**
```
- Ground Check Distance: 0.3 → 0.5
- Ground Check Radius: 0.2 → 0.3
- Cek CapsuleCollider Center Y
```

### **3. Ground Detection Tidak Berfungsi**

**Penyebab:**
- Layer mask salah
- Platform tidak punya collider

**Solusi:**
```
A. Pilih karakter, lihat Gizmos:
   - Sphere selalu merah = tidak detect lantai
   
B. Pastikan:
   - Platform punya collider ✓
   - Platform layer = Ground ✓
   - GerakKarakter Ground Layer = Ground only ✓
```

### **4. Karakter Terbalik/Jatuh**

**Penyebab:**
- Freeze Rotation tidak di-set

**Solusi:**
```
Rigidbody → Constraints → Freeze Rotation:
  X: ✓ (checked)
  Y: ✗ (unchecked)
  Z: ✓ (checked)
```

### **5. Karakter Jittering/Bergetar**

**Penyebab:**
- Rigidbody interpolation tidak aktif
- Velocity snap threshold terlalu besar

**Solusi:**
```
A. Rigidbody → Interpolate: Interpolate

B. GerakKarakter:
   - Velocity Snap Threshold: 0.5 → 0.3
```

### **6. Tidak Bisa Double Jump**

**Penyebab:**
- Ground detection gagal
- Flag tidak ter-reset

**Solusi:**
```
A. Pastikan ground detection berfungsi (cek Gizmos)
B. Pastikan mendarat dulu sebelum jump lagi
C. Test di platform yang pasti ter-detect
```

---

## 📊 Parameter Tuning

### **Jika karakter masih "floaty" (melayang):**
```
Gravitasi: -20 → -30
Kekuatan Lompat: 8 → 7
Velocity Snap Threshold: 0.5 → 0.2
```

### **Jika karakter terlalu "heavy" (berat):**
```
Gravitasi: -20 → -15
Kekuatan Lompat: 8 → 10
```

### **Jika ground detection tidak sensitif:**
```
Ground Check Distance: 0.3 → 0.5
Ground Check Radius: 0.2 → 0.3
```

### **Jika ground detection terlalu sensitif (detect terlalu jauh):**
```
Ground Check Distance: 0.3 → 0.2
Ground Check Radius: 0.2 → 0.15
```

---

## 🎯 Nilai Recommended (Tested)

```
[Rigidbody]
Mass: 1
Interpolate: Interpolate
Collision Detection: Continuous Dynamic

[CapsuleCollider]
Center Y: 1.0 (untuk model tinggi 2m)
Radius: 0.3
Height: 2.0

[GerakKarakter - Fisika]
Gravitasi: -20
Kekuatan Lompat: 8

[GerakKarakter - Ground Detection]
Ground Check Radius: 0.25
Ground Check Distance: 0.35
Velocity Snap Threshold: 0.5
Ground Layer: HANYA Ground

[GerakKarakter - Movement]
Kecepatan Lari: 6
Kecepatan Udara Multiplier: 0.8
```

---

## 🎮 Kontrol

- **WASD**: Gerak (otomatis lari)
- **Spasi (1x)**: Jump
- **Spasi (2x di udara)**: Double jump salto

---

## 💡 Tips Pro

1. **Selalu gunakan Gizmos debug** untuk troubleshooting
2. **Setup layer "Ground" dengan benar** - ini kunci utama!
3. **Adjust collider sesuai model** - setiap model beda ukuran
4. **Test di Scene view sambil lihat Gizmos** - lebih mudah debug
5. **Gunakan Continuous Dynamic collision** untuk fast movement
6. **Freeze rotation X dan Z** untuk mencegah karakter terbalik

---

## ✅ Checklist Setup Final

```
□ Karakter punya Rigidbody (Use Gravity = OFF)
□ Karakter punya CapsuleCollider (sesuaikan ukuran)
□ Rigidbody Freeze Rotation X & Z (checked)
□ Rigidbody Interpolate = Interpolate
□ Layer "Ground" sudah dibuat
□ Semua platform/lantai layer = Ground
□ Semua platform punya Collider (Is Trigger = OFF)
□ Semua platform punya script ObjekTembok
□ GerakKarakter Ground Layer = Ground only
□ Test: Sphere debug hijau saat di lantai
□ Test: Tidak tenggelam saat idle
□ Test: Bisa jump dan mendarat smooth
```

Jika semua checklist ✓, sistem fisika sudah sempurna! 🎉
