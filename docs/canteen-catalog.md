# Menu per kantin

Katalog lokal berisi tiga kantin kampus dengan menu makanan/minuman sungguhan dan
opsi porsi, pedas, tambahan, ukuran, gula, atau es sesuai tiap hidangan.

| Kantin | Lokasi | Jumlah penawaran |
| --- | --- | ---: |
| A · Taman Rasa | Gedung A · area taman utama | 17 |
| B · Dapur Teknik | Gedung B · lantai dasar | 9 |
| C · Sudut Pustaka | Gedung C · dekat perpustakaan | 8 |

Nama/lokasi, harga, stok, dan ulasan adalah data dummy yang bisa diganti di
`Models/CanteenModels.cs` dan `Data/SeedData.cs`. Contohnya Nasi Ayam Geprek dijual
di A dan B dengan ID penawaran serta harga berbeda. Es Teh Manis dan Paket Hemat
Siang tersedia di ketiga kantin. Mie Ayam Bakso dan Gado-Gado tersedia di A dan C.

`IMenuService.All` memuat semua penawaran; `Query` dan `GetHighlights` menerima
`canteenId` agar pencarian, kategori, urutan, pilihan populer, dan promo tetap
berasal dari kantin yang dipilih. ID penawaran lama dipertahankan untuk Kantin A;
penawaran B/C memakai prefix seperti `kantin-b:geprek`. Ikon makanan diambil dari
ID hidangan di belakang prefix.

`ICanteenSelectionService` menyimpan kantin pilihan dan membagikan perubahan ke
Beranda/Menu. Mengganti kantin untuk melihat katalog tidak menghapus keranjang.
Satu keranjang berisi satu kantin; penambahan dari kantin lain meminta konfirmasi
penggantian sebelum mutasi apa pun. Opsi dan harga unit milik penawaran yang
dipilih disimpan bersama `CanteenId`, jumlah, dan catatan.

Untuk mengganti data lokal dengan backend, implementasikan `IMenuService`, ganti
katalog lokasi, dan pertahankan ID penawaran yang unik per kantin. Nama hidangan
yang sama tidak boleh dipakai sebagai kunci identitas keranjang.

Tes katalog/keranjang menggunakan service dan serializer produksi, tanpa HP:

```powershell
dotnet run --project Tests/Catalog/KantinKita.Catalog.Tests.csproj
```
