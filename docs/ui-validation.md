# Validasi UI Android — 26 Oktober 2026

Diuji pada HP Xiaomi 23021RAA2Y, Android 15 / API 35, layar 1080 × 2400.
Catatan ini memuat beberapa iterasi; hasil dan metrik lama diberi konteks revisi.
APK Release arm64 memakai profiled AOT dan R8 serta dipasang langsung ke perangkat.
Navbar kini memakai panel kaca berwarna lembut agar tetap terbaca, dengan indikator
lensa, kilau, dan deformasi native. Filter kategori memakai satu baris teks dan urutan
Menu dipilih melalui satu tombol.
Efek kaca ini tidak memakai blur/refraksi backdrop. Ikon memakai 47 SVG resmi
Microsoft Fluent System Icons dari commit yang dicatat di manifest vendored.

## Revisi pencarian, login, picker visual, dan AMOLED

Keputusan layout, tipografi, warna, pilihan, dialog, serta motion dicocokkan ke
[panduan resmi Microsoft Fluent 2](fluent2-design-review.md). Pemeriksaan interaksi
di bawah dilakukan langsung pada HP melalui build Debug. Build Release arm64
akhir selesai dengan **0 warning dan 0 error**, berukuran **14.949.092 byte**.
Tidak ada pengukuran FPS/frame time baru pada revisi ini.

APK Release akhir dipasang dengan pembaruan aplikasi yang mempertahankan data.
Smoke test sesudah AOT/trimming memverifikasi Enter pada Beranda menutup IME dan
menempatkan header hasil pada batas layar `[55,232][887,309]`. Enter pada Menu
menutup IME dengan posisi vertikal pencarian tetap pada y=382. Picker Release
menolak meja B-02 yang terisi; pemilihan B-01 diikuti **Gunakan meja** menerapkan
Meja B-01 · 4 kursi. Memilih Gedung C lalu Batal mempertahankan teks **Pilih gedung
tujuan** pada checkout. Profil tamu tetap menampilkan enam pesanan pengguna dan
saldo Rp160.500 yang ada saat pemeriksaan.

- **69 tes regresi lolos:** 15 gesture, 8 view model/kontrol, 12 katalog/keranjang,
  14 transaksi/struk/tujuan, 4 notifikasi, 7 sesi lokal, dan 9 pilihan visual.
- Enter pada pencarian Beranda menutup IME, menunggu inset keyboard selesai,
  lalu menempatkan header hasil pencarian di area terlihat. Pencarian `ayam`
  menampilkan empat hasil pada kantin yang sedang dipilih.
- Enter pada pencarian Menu menutup IME dan mempertahankan posisi vertikal
  pencarian. Perubahan batas horizontal saat hilang fokus berasal dari animasi
  border; hasil tetap terlihat tanpa scroll otomatis.
- Login baru diperiksa pada terang dan AMOLED, termasuk posisi ikon/teks input,
  CTA dan jalur tamu. Native sans-serif mengikuti platform Android.
- Picker meja Kantin B memperlihatkan lima dari delapan meja kosong; dua meja
  simulasi dan satu meja dari pesanan pengguna aktif berstatus terisi. Meja terisi
  terdeteksi disabled pada hierarki UI Android. Memilih B-01 tidak menutup panel;
  **Gunakan meja** baru menerapkan label Meja B-01 · 4 kursi ke checkout.
- Memilih Gedung C lalu Batal mempertahankan tujuan checkout sebelumnya. Membuka
  kembali dan menekan **Gunakan gedung** menerapkan Gedung C.
- Varian Nasi Ayam Geprek memisahkan label dan harga dalam tile ringkas.
  Memilih Pedas banget (+Rp1.000) dan Telur ceplok (+Rp3.000) memperbarui CTA dari
  harga dasar Rp16.000 menjadi **Tambah · Rp20.000**. Item detail ini tidak ditambahkan.
- Toggle Mode AMOLED mengubah kanvas dan system bar menjadi hitam. Sampel piksel
  screenshot login pada kanvas, status bar, dan navigation bar masing-masing
  bernilai RGB **(0, 0, 0)**. Surface konten tetap memiliki tingkat gelap tersendiri.
- Perpindahan tab setingkat memakai fade 180ms. Artwork dan feedback komponen
  mempertahankan gerak terpisah serta mengikuti pengaturan animasi Android.
- Pada masing-masing QA Debug dan Release, satu Es Teh Manis sementara ditambahkan
  untuk membuka checkout setelah keranjang awal dipastikan kosong, lalu dihapus
  lewat konfirmasi aplikasi. Tidak menekan
  Buat pesanan, top up saldo, atau memilih penerima berbagi selama pemeriksaan.
- Hash riwayat pesanan sebelum/sesudah pemeriksaan sama, keranjang kembali kosong,
  sesi tamu, tema terang, dan pilihan Kantin B awal dipulihkan. Pesanan pengguna
  tetap dipertahankan. Tidak ada crash pada log pemeriksaan.

Preview permanen: [login terang](previews/fluent-login-light.png),
[login AMOLED](previews/fluent-login-amoled.png),
[meja](previews/fluent-table-plan-light.png),
[gedung](previews/fluent-building-picker-light.png),
[opsi makanan](previews/fluent-food-options-light.png), dan
[Beranda AMOLED](previews/amoled-home.png). Screenshot percobaan/helper dan output
build/tes dibersihkan setelah QA; sumber serta preview dokumentasi tetap disimpan.

## Render navigasi — sampel iterasi sebelum login

Sampel berikut dicatat pada iterasi struk/notifikasi sebelum perubahan login dan
dialog custom; hasil ini bukan pengukuran ulang revisi terbaru.
Pengukuran `adb shell dumpsys gfxinfo com.kantinkita.app` dilakukan setelah
keempat tab pernah dibuka. Delapan perpindahan Menu → Pesanan → Profil → Beranda
(dua putaran), jeda perintah 0,7 detik, tanpa screen recording selama pengukuran.
Pengukuran dilakukan dengan keranjang kosong dan transaksi tersimpan pada tab Pesanan.

| Metrik | Hasil |
| --- | ---: |
| Frame yang dirender | 287 |
| Frame melewati deadline | 9 (3,14%) |
| Median | 12 ms |
| Persentil 90 | 27 ms |
| Persentil 95 | 97 ms |
| Persentil 99 | 200 ms |

Masih ada lonjakan saat pergantian; sampel singkat ini tidak menjamin seluruh
kondisi perangkat. Pembukaan tab pertama juga memerlukan pembuatan native view.
Konfigurasi Debug memakai interpreter, sehingga tidak setara untuk pengukuran ini.

Transisi memakai animator native, tab yang sudah dibuka mempertahankan ukuran,
dan lapisan penahan sentuhan sementara menggantikan perubahan input berantai
ke seluruh anak halaman. Kartu promo tetap menangani pembatalan gesture sendiri.

## Pemeriksaan perilaku

- Swipe promo setelah native Cancel dan setelah scroll vertikal tetap dapat berpindah.
- Tap tab beruntun berakhir di tab terakhir tanpa lapisan input yang tertinggal.
- Popup bel muncul di kanan atas sambil tetap berada di Beranda. Tombol Back dan
  tombol tutup menutup popup. Aktivitas memakai transaksi tersimpan; tidak ada isi contoh.
- Pada iterasi struk/notifikasi, urutan Menu masih membuka pilihan native.
  Revisi login menggantinya dengan sheet custom yang mengikuti tinggi isi.
- Transaksi yang dibuat di aplikasi tetap tersedia setelah install Release dan restart.
  Tombol Lihat struk membuka kode, rincian, metode pembayaran dan total yang sama.
- Pilihan berbagi PDF Android terbuka; pengujian dibatalkan tanpa memilih penerima.
- PDF uji 40 baris menghasilkan 10 halaman. Seluruh 40 nama item terbaca dan tidak
  ada karakter keluar batas halaman. PNG halaman pertama dan terakhir diperiksa visual.
- Data uji PDF tidak ditempatkan di riwayat, saldo, atau keranjang. Fixture dan hook
  pengujian Android sementara dihapus sebelum build Release akhir.
- Tema terang/gelap, warna system bar, dan indikator navbar diperiksa visual.
- Tidak ada crash tercatat selama pemeriksaan Release.
- 43 regresi lolos: 15 gesture, 8 view model/kontrol, 4 notifikasi, 9 transaksi/struk,
  dan 7 profil lokal,
  termasuk riwayat rusak, penulisan gagal, ID unik, dan pemulihan data setelah restart.
- Build Release selesai dengan 0 warning dan 0 error. Lisensi Fluent Icons
  terverifikasi pada `assets/licenses/fluent-system-icons.txt` dalam APK.

Preview dan rekaman berada di `docs/previews/`. Tema gelap awal dipulihkan dan
transaksi pengguna dipertahankan setelah pemeriksaan. File PDF uji di cache aplikasi
dan output build/tes sementara dibersihkan setelah QA; sumber serta preview dokumentasi
tetap disimpan.

## Revisi login, dialog custom, dan promo tanpa zoom

- Build Debug dan Release arm64 selesai dengan 0 warning/error. Semua 43 tes lolos.
- Login memvalidasi input secara inline. Profil uji tersimpan dan sapaan di Beranda
  mengikuti nama yang diisi. Force-stop dan pembukaan ulang mempertahankan sesi.
- Keluar menampilkan konfirmasi custom, mengembalikan layar login, lalu opsi tamu
  dapat masuk kembali. Identitas uji dibuang dan mode tamu awal dipulihkan.
- Login diperiksa visual pada tema terang/gelap. Toggle tema custom berhasil
  mengubah tema dan tema gelap awal dipulihkan setelah pemeriksaan.
- Pilihan rating pada sheet custom memperbarui pengurutan katalog. Panel
  diukur dari viewport native agar tinggi mengikuti isi, dengan scroll saat isi
  melampaui layar; tombol Back menutupnya dan pilihan lain dapat dibuka kembali.
- Swipe pendek mempertahankan promo. Swipe panjang berpindah satu posisi;
  enam swipe beruntun, swipe balik, dan swipe setelah scroll vertikal lolos.
- Kedua template promo memiliki geometri dan format teks yang sama. Native scale
  dijaga 1.0 saat drag/release/handoff; efek gerak memakai tilt/lift/parallax kecil.
  Preview yang terlihat dipertahankan sampai front pengganti melewati dua frame.
- Garis kilau statis di atas ikon navbar dihapus; indikator biru bawah dipertahankan.
- Tidak membuat pesanan, top up saldo, atau mengirim file selama QA revisi ini.
  Kondisi perangkat saat QA: mode tamu, saldo Rp137.500, riwayat kosong.
- `scripts/` dan `.csproj.filters` yang tidak digunakan C# dihapus. File nesting
  proyek ditulis dalam `.filenesting.json`; panduan dan sumber Microsoft ada di
  [project-organization.md](project-organization.md).
- Smoke test Release setelah AOT/trimming memverifikasi swipe promo, pemilihan
  Termurah pada sheet, popup notifikasi dan Back. Tidak ada crash pada log pengujian.
- Rekaman [promo-motion.mp4](previews/promo-motion.mp4) menunjukkan swipe panjang,
  swipe pendek yang kembali, swipe balik dan beberapa swipe cepat pada Release.

Preview login, pengaturan, dan sheet disimpan sebagai dokumentasi permanen di
`docs/previews/`; screenshot percobaan, helper ADB, dan output build/tes dibersihkan.

### Sampel render revisi login/dialog

Release arm64, empat tab dihangatkan lebih dahulu, delapan perpindahan tab dengan
jeda perintah 0,7 detik, tanpa screen recording. Mode tamu dan riwayat kosong.

| Metrik | Hasil |
| --- | ---: |
| Frame yang dirender | 389 |
| Janky frames (metrik Android saat ini) | 10 (2,57%) |
| Janky frames (legacy) | 140 (35,99%) |
| Median | 13 ms |
| Persentil 90 | 19 ms |
| Persentil 95 | 38 ms |
| Persentil 99 | 150 ms |

Masih ada lonjakan frame time. Ini sampel singkat pada satu perangkat, dengan data
yang berbeda dari iterasi sebelumnya; tidak menjadi eksperimen perbandingan terkontrol.

## Revisi katalog kantin, meja, dan indikator promo

- 60 tes lolos: 15 gesture, 8 view model/kontrol, 4 notifikasi, 7 sesi lokal,
  14 transaksi/struk/tujuan, dan 12 katalog/keranjang. Build Debug dan Release
  arm64 selesai dengan 0 warning/error.
- Field Nama/Email memakai kolom ikon 24dp yang sama, ikon 20dp, gap 12dp,
  serta teks yang dipusatkan secara native tanpa padding font Android ekstra.
  Email memakai ikon Mail resmi Microsoft. Screenshot terang diperiksa visual.
- Indikator lama berupa teks diganti track titik dengan pill native yang bergerak
  340ms. Pada Release, swipe mengubah semantik Promo 1 → 2 dan tombol sebelumnya
  mengembalikannya ke Promo 1. Rekaman promo terbaru menyertakan indikator ini.
- Pilihan kantin disinkronkan antara Home dan Menu. Kantin B menampilkan 9 menu;
  Kantin C menampilkan 8. Promo, harga, kategori dan pencarian mengikuti lingkup kantin.
- QA menambah item sementara hanya setelah memastikan keranjang awal kosong.
  Menolak perpindahan mempertahankan kantin C; menyetujui menggantinya satu kali
  dengan item kantin B. Keranjang QA kemudian dikosongkan dan kantin A dipulihkan.
- Checkout berhasil memilih Meja B-01 dari enam meja simulasi tersedia; dua meja
  terisi tidak ditawarkan. Mode Antar menerima Gedung C dan Ruang 302.
  QA tidak menekan tombol pembuatan pesanan atau mendebit saldo perangkat.
- Pesanan pengguna yang sudah ada dipertahankan. Hash berkas riwayat sebelum/sesudah
  pemeriksaan login sama; transaksi juga tetap terlihat setelah pemasangan Release.
  Sesi tamu dan tema terang yang dipakai saat QA dipertahankan.
- Tes domain memverifikasi tujuan antar tetap sama setelah restart dan pada struk,
  meja aktif tidak dapat diklaim dua kali, tujuan beda kantin/ruangan kosong ditolak,
  serta JSON lama tetap dapat dibuka. PDF memakai snapshot tujuan yang sama.
- Tidak ada crash pada log pemeriksaan. File build dan helper/screenshot percobaan
  dibersihkan; preview terpilih serta rekaman di `docs/previews/` adalah dokumentasi permanen.
