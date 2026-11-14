# Profil lokal dan integrasi backend

Layar masuk membuat profil di perangkat, dengan nama panggilan dan email. Email belum diverifikasi. Tidak ada password, autentikasi server, atau kredensial contoh yang dianggap sah. Pilihan tamu juga membentuk sesi lokal, sehingga aplikasi dapat dibuka kembali tanpa mengisi formulir.

`SessionService` menyimpan mode, nama, dan email melalui `IPreferences`. Penanda sesi ditulis terakhir, setelah validasi dan provider berhasil. Keluar hanya menghapus sesi profil; riwayat transaksi, keranjang, dan pengaturan aplikasi tetap ada di perangkat. Semua data aplikasi saat ini masih satu kumpulan lokal, bukan basis data per akun.

## Mengganti provider

1. Implementasikan `ISignInProvider`, menggantikan `LocalSignInProvider` pada registrasi dependency injection di `MauiProgram.cs`.
2. `SignInAsync` harus menyelesaikan autentikasi sungguhan terlebih dahulu, misalnya membuka alur OAuth melalui browser. Kembalikan `SignInIdentity` berdasarkan identitas dari server. `SignInRequest` merupakan data awal formulir, bukan bukti autentikasi.
3. Simpan token melalui penyimpanan kredensial yang sesuai, misalnya `SecureStorage`, bukan `IPreferences`. Penanda profil lokal tidak boleh dipakai untuk memutuskan akses server.
4. Sesuaikan teks dan field di `LoginPage` dengan alur server. Tambahkan restore/refresh/revoke token pada service autentikasi nyata; penanda `HasSession` saat ini hanya bermakna profil lokal tersedia.
5. Sebelum mendukung banyak akun, pisahkan atau sinkronkan riwayat pesanan, saldo, favorit, dan keranjang berdasarkan identitas server. `SignOutAsync` saat ini tidak menghapus data transaksi perangkat.

Provider sengaja dipisahkan dari penyimpanan sesi dan dari view model. `LoginViewModel` baru menerbitkan `Completed` setelah provider berhasil dan profil tersimpan. `LoginPage.SessionStarted` diterbitkan setelah animasi keluar; aplikasi dapat mengganti root navigasi di titik tersebut.
