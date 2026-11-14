# Microsoft Fluent System Icons

Subset resmi dari [microsoft/fluentui-system-icons](https://github.com/microsoft/fluentui-system-icons),
dipatok pada commit `82ecf7cbdb43684403c7125efa160be04a33dca5` (23 Oktober 2026).
Lisensi MIT lengkap ada di [LICENSE](LICENSE) dan ikut disertakan sebagai aset aplikasi.

`manifest.json` mencatat setiap SVG asal beserta alias nama aplikasi. SVG asli
disimpan tanpa perubahan di `svg/`; aplikasi memakai path yang dihasilkan dalam
`Controls/Icons.cs`, sehingga tidak membutuhkan font ikon atau unduhan saat runtime.
Warna SVG diganti dengan warna tema saat dirender; geometri regular menggunakan
fill, karena bentuk garisnya sudah menjadi bagian dari path resmi. Status selected
dan favorit memakai varian filled resmi bila tersedia.

Jika menambah ikon, ambil SVG regular/filled dari commit yang tercatat, perbarui
manifest, lalu salin geometri path ke `Controls/Icons.cs` dengan aturan fill SVG
yang sama. Folder generator `scripts` telah dihapus sesuai kebutuhan proyek.
