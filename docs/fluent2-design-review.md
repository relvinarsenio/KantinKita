# Keputusan UI dan referensi Fluent 2

Revisi pencarian, katalog, login, opsi makanan, pemilihan tujuan, dan AMOLED diperiksa
terhadap situs resmi Microsoft Fluent 2. Implementasi memakai kontrol MAUI Android
dan geometri Microsoft Fluent System Icons yang sudah disimpan di proyek.

| Keputusan | Penerapan pada aplikasi | Panduan resmi |
| --- | --- | --- |
| Ritme layout | Login, grup opsi, dan picker memakai jarak 4/8/12/16/24dp; informasi terkait dikelompokkan dengan ruang. | [Layout](https://fluent2.microsoft.design/layout) |
| Tipografi Android | Native sans-serif (`sans-serif`, `sans-serif-medium`; Roboto pada Android standar, font OEM dapat berbeda), caption baru 12sp, body/pilihan 14sp, heading 20/24/34sp; label baru memakai sentence case. | [Typography](https://fluent2.microsoft.design/typography) |
| Login | Heading utama terpisah dari form lokal, satu CTA utama, jalur tamu sekunder, artwork vektor dekoratif yang tidak memuat informasi wajib. | [Layout](https://fluent2.microsoft.design/layout), [Card](https://fluent2.microsoft.design/components/web/react/core/card/usage) |
| Kategori dan urutan | Selector berlabel menampilkan pilihan aktif; panel custom dapat digulir saat kategori bertambah. Barisan kategori tidak dipadatkan menjadi banyak tab kecil pada viewport HP. | [Dropdown](https://fluent2.microsoft.design/components/web/react/core/dropdown/usage), [Tablist](https://fluent2.microsoft.design/components/web/react/core/tablist/usage) |
| Pencarian Beranda | Satu bagian hasil menampilkan semua kecocokan saat pencarian/filter aktif; promo disembunyikan agar hasil mendapat prioritas. Tampilan jelajah awal dibatasi enam menu dengan CTA katalog. Tombol hapus hanya terlihat jika ada teks. | [Searchbox](https://fluent2.microsoft.design/components/web/react/core/searchbox/usage), [Layout](https://fluent2.microsoft.design/layout) |
| Kartu katalog dan pesanan | Kartu katalog mengelompokkan ilustrasi, judul dan metadata, lalu memisahkan harga serta aksi Tambah dalam footer. Kartu pesanan menempatkan total, metadata, dan aksi pada baris berbeda agar tidak bertumpuk. | [Card](https://fluent2.microsoft.design/components/web/react/core/card/usage) |
| Varian makanan | Grup pilihan tunggal dengan default logis pertama, nama tidak dipotong, biaya terpisah, penanda dan deskripsi pilihan; dua kolom membungkus ketika ruang memungkinkan. | [Radio group](https://fluent2.microsoft.design/components/web/react/core/radiogroup/usage) |
| Meja dan gedung | Pilihan visual tetap provisional sampai konfirmasi; meja terisi tampil disabled dengan teks status dan diagram kursi. Ini representasi pilihan, bukan peta geografis atau status server langsung. | [Card](https://fluent2.microsoft.design/components/web/react/core/card/usage), [Radio group](https://fluent2.microsoft.design/components/web/react/core/radiogroup/usage) |
| Panel tujuan | Judul dan footer menetap; hanya opsi yang scroll. Batal/Back membuang perubahan. Tidak ada X tambahan ketika tombol Batal sudah tersedia. Fokus sebelumnya dipulihkan setelah tutup. | [Dialog](https://fluent2.microsoft.design/components/web/react/core/dialog/usage) |
| Motion | Fade 180ms untuk tab yang setingkat. Pergeseran/scale kecil hanya pada komponen dan artwork, dengan pembatalan serta penghormatan pengaturan animasi sistem. | [Motion](https://fluent2.microsoft.design/motion) |
| AMOLED dan warna | Kanvas/system bar hitam `#000000`; surface bertingkat tetap gelap. Label, status teks, dan checkmark mendampingi warna. Metadata terang diperkuat kontrasnya. | [Color](https://fluent2.microsoft.design/color), [Accessibility](https://fluent2.microsoft.design/accessibility) |

Panduan [Tablist](https://fluent2.microsoft.design/components/web/react/core/tablist/usage)
menyarankan alternatif seperti dropdown ketika ruang sempit membuat banyak tab
masuk overflow. Selector kategori/urutan menerapkan prinsip itu: tinggi area filter
tetap ringkas, sedangkan opsi dapat bertambah di panel yang digulir. Panduan
[Dropdown](https://fluent2.microsoft.design/components/web/react/core/dropdown/usage)
mendukung filter/urut dengan pilihan aktif dan label deskriptif; panduan yang sama
menyarankan mempertimbangkan select native untuk aksesibilitas form mobile.
Di aplikasi ini panel MAUI custom mempertahankan label pilihan, status terpilih,
opsi disabled, dan Back/Batal. Referensi React dipakai sebagai panduan perilaku dan
hierarki; ini bukan penggunaan komponen React atau klaim kesetaraan aksesibilitas
dengan select native.

Warna merupakan adaptasi brand aplikasi melalui token semantik. Rasio metadata
`#5F6B7A` terhadap surface muted `#ECEFF5` sekitar 4,71:1. Copy pucat pada ujung
gradien brand `#245BCA` sekitar 4,93:1. Angka ini pemeriksaan pasangan warna tersebut,
bukan klaim sertifikasi aksesibilitas seluruh aplikasi.

Android mendukung font system lewat `FontFamily`; lihat
[Microsoft: Fonts in .NET MAUI](https://learn.microsoft.com/en-us/dotnet/maui/user-interface/fonts?view=net-maui-10.0).

Interaksi Enter menggunakan `HideSoftInputAsync` yang didokumentasikan Microsoft:
[Entry / soft input](https://learn.microsoft.com/en-us/dotnet/maui/user-interface/controls/entry?view=net-maui-10.0).
Beranda menunggu inset keyboard berakhir sebelum scroll ke header hasil; Menu
cukup menutup keyboard, karena hasil sudah berada tepat di bawah pencarian.
