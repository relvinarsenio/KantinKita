# Pesanan dan struk

Halaman Pesanan sekarang menampilkan transaksi yang dibuat lewat checkout.
Daftar contoh bawaan telah dihapus; membuka aplikasi tidak membuat transaksi.
Pesanan baru disimpan ke `FileSystem.AppDataDirectory/kantin-orders.json` memakai
JSON source generation, file sementara, lalu penggantian atomik. Nomor urut ikut
disimpan dan penulisan diproses berurutan untuk mencegah benturan kode.

Checkout menunggu riwayat siap dan penulisan pesanan berhasil sebelum mengosongkan
keranjang. Jika penyimpanan gagal, pesanan tidak dipublikasikan dan debit saldo
lokal pada percobaan itu dikembalikan. File riwayat yang tidak bisa dibaca tetap
dipertahankan, dengan tombol muat ulang di halaman Pesanan.

Setiap transaksi memiliki tombol **Lihat struk**, termasuk pada halaman selesai
checkout. Struk memakai snapshot transaksi: kode, tanggal/jam dengan zona waktu,
warung, item, opsi, catatan, jumlah × harga satuan, total baris, subtotal, layanan,
voucher/diskon, total akhir, metode pembayaran dan waktu pengambilan. Tidak ada
kasir, alamat, pajak, atau status lunas yang dibuat tanpa data pendukung.

Status memakai nilai yang tersimpan pada pesanan. Perjalanan dapur otomatis
berdasarkan waktu telah dihapus; integrasi layanan dapur di masa depan dapat
memperbarui status secara eksplisit. Metode pembayaran yang ditampilkan adalah
pilihan checkout, bukan konfirmasi pembayaran eksternal.

**Bagikan struk PDF** menghasilkan dokumen native Android yang dapat dicari dan
memiliki halaman lanjutan untuk pesanan panjang. File berada di cache aplikasi;
Android menampilkan pilihan berbagi setelah pengguna menekan tombol tersebut.
Aplikasi tidak memilih penerima atau mengirim file secara otomatis.

Tes regresi untuk riwayat, nomor urut, kegagalan penyimpanan, data keranjang yang
berubah, JSON source generation dan rincian struk:

```powershell
dotnet run --project Tests/Orders/KantinKita.Orders.Tests.csproj
```
