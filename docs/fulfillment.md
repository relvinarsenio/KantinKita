# Meja dan tujuan pesanan

Checkout menyimpan cara menerima pesanan dalam snapshot immutable `OrderDestination`:

- `Pickup`: ambil sendiri di kantin asal keranjang.
- `DineIn`: kantin, ID meja, dan label meja yang dipilih.
- `Delivery`: kantin asal, gedung tujuan, dan nomor/nama ruangan.

Tujuan ikut tersimpan bersama transaksi, ditampilkan pada riwayat, halaman sukses,
struk, dan PDF. Riwayat lama tanpa `Destination` tetap dapat dibuka sebagai
pengambilan sendiri di kantin asal item. Satu pesanan hanya mencakup satu kantin;
service memeriksa kesesuaian kantin tujuan dengan semua baris keranjang dan memakai
nama kantin kanonis saat membuat snapshot.

## Ketersediaan meja lokal

`ITableAvailabilityProvider` merupakan titik penggantian feed pengelola kantin.
Implementasi `LocalTableAvailabilityProvider` berisi delapan meja per kantin,
dua di antaranya ditempati sebagai data simulasi. Pesanan makan di tempat yang
masih aktif pada perangkat juga membuat meja terkait tidak dapat dipilih lagi.
Order service menolak klaim meja aktif yang sama di dalam semaphore penulisan,
sehingga pemesanan lokal bersamaan tidak membuat dua klaim meja yang sama.

UI memberi label **Simulasi ketersediaan meja** karena status ini tidak disinkronkan
dengan pelanggan atau petugas pada perangkat lain. Data ini tidak membuktikan
kondisi meja fisik dan tidak membuat reservasi pada sistem kampus.

Picker meja menampilkan seluruh meja dalam tile dengan diagram kursi, kapasitas,
dan label Kosong/Terisi. Meja yang terisi tetap terlihat dengan pilihan disabled.
Picker gedung memakai kartu dengan ikon pin. Pilihan pada kedua picker bersifat
sementara sampai tombol **Gunakan meja** atau **Gunakan gedung** ditekan; Batal,
Back, atau menutup panel mempertahankan tujuan checkout sebelumnya. Judul dan
tombol tetap terlihat sementara isi dapat digulir. Layout beralih ke satu kolom
ketika ruang atau ukuran font memerlukannya.

Untuk backend nyata, ganti `ITableAvailabilityProvider`, ambil daftar gedung dari
data kampus, lalu lakukan pengecekan dan reservasi meja secara atomik pada API
pembuatan pesanan. Status selesai/batal dari backend harus memperbarui daftar
pesanan agar meja dapat tersedia lagi. Endpoint backend juga perlu memutuskan
jangkauan pengantaran dan biaya; revisi lokal ini tidak menambahkan biaya antar
yang belum ditetapkan, dan biaya layanan yang ada tetap tersimpan di transaksi.

## Validasi

Gedung dan ruangan wajib untuk pengantaran. Meja harus tersedia ketika checkout
dikonfirmasi. Tujuan yang tidak valid ditolak sebelum pesanan diterbitkan; kegagalan
penyimpanan tidak menghapus keranjang. JSON riwayat dengan tujuan rusak ditolak
dengan mempertahankan berkas asli.

`Tests/Orders` mencakup tujuan pengantaran setelah restart, pemetaan struk, meja
aktif lokal, penolakan tujuan berbeda kantin/ruangan kosong, kompatibilitas JSON
lama, dan perlindungan riwayat berisi tujuan yang tidak valid.

`Tests/Choices` menguji pilihan sementara, tujuan disabled/tidak dikenal, pembatalan,
konfirmasi satu kali, dan kumpulan pilihan kosong. Keputusan visual dan interaksi
mengacu pada [panduan resmi Fluent 2](fluent2-design-review.md).
