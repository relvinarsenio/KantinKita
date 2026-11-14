# Konfigurasi build dan performa Android

Audit 26 Oktober 2026. Target aplikasi tetap .NET 10 Android dengan runtime Mono. Evaluasi properti MSBuild dilakukan tanpa build, restore, install, atau deployment.

## Toolchain yang ditemukan

- .NET SDK: 10.0.402.
- .NET MAUI: 10.0.110.
- Android SDK workload: Microsoft.Android.Sdk.Windows 36.1.118.
- Target: net10.0-android; minimum Android API 24.

## Optimasi yang sudah aktif

Nilai berikut berasal dari evaluasi Release project lokal, bukan asumsi dari template:

| Properti | Nilai |
| --- | --- |
| MauiXamlInflator | SourceGen |
| RunAOTCompilation | true |
| AndroidEnableProfiledAot | true |
| PublishTrimmed / TrimMode | true / partial |
| AndroidEnableMarshalMethods | True |
| AndroidEnablePreloadAssemblies | False |
| AndroidAotEnableLazyLoad | True |
| AndroidUseAssemblyStore | true |
| AndroidEnableAssemblyCompression | True |
| AndroidAvoidEmitForPerformance | true |
| Optimize | true |

XAML project telah menggunakan source generation dan x:DataType untuk compiled bindings. Source generation menggunakan kode yang sama pada Debug dan Release; tidak perlu menambahkan kembali switch yang sudah aktif. [Microsoft: XAML processing](https://learn.microsoft.com/en-us/dotnet/maui/xaml/xamlc?view=net-maui-10.0).

Dokumentasi umum Android terus diperbarui untuk .NET berikutnya. Untuk project ini, default aktual SDK lokal menjadi acuan; misalnya marshal methods sudah aktif walaupun halaman referensi umum menyebut default lain. [Microsoft: Android build properties](https://learn.microsoft.com/en-us/dotnet/android/building-apps/build-properties).

## Perubahan yang diterapkan

Release sekarang menetapkan AndroidLinkTool=r8. Sebelum perubahan, nilainya kosong. R8 menghapus bytecode Java/AndroidX yang tidak diperlukan; ini melengkapi managed trimming yang sudah aktif. SDK lokal mengenali r8, mengaktifkan Java shrinking, dan memasukkan generated JNI keep rules beserta konfigurasi library. Debug tetap memakai konfigurasi pengembangan sebelumnya. [Microsoft: AndroidLinkTool](https://learn.microsoft.com/en-us/dotnet/android/building-apps/build-properties#androidlinktool), [dotnet/android: integrasi D8/R8](https://github.com/dotnet/android/blob/main/Documentation/guides/D8andR8.md).

Manfaat yang diharapkan adalah pengurangan DEX/package size. Java shrinking dapat menambah waktu build. Tidak ada klaim bahwa R8 memperbaiki FPS atau startup; hal itu membutuhkan pengukuran terpisah.

Lisensi MIT Microsoft Fluent System Icons dikemas sebagai MauiAsset dengan nama licenses/fluent-system-icons.txt. SVG vendored tetap menjadi sumber derivasi geometry dan tidak didaftarkan sebagai MauiImage.

## Pilihan yang dipertahankan

- Profiled AOT dan partial trimming tetap mengikuti SDK. Full trimming memerlukan audit trim warnings serta pengujian seluruh alur sebelum diaktifkan.
- Tidak mengaktifkan CoreCLR/NativeAOT Android eksperimental atau LLVM AOT yang memerlukan evaluasi toolchain dan pengukuran terpisah.
- Tidak mengaktifkan invariant globalization: aplikasi memakai format dan culture Indonesia.
- Tidak menambahkan MauiEnableXamlCBindingWithSourceCompilation karena saat audit tidak ada explicit Binding.Source/x:Reference yang mendapat manfaat langsung. [Microsoft: compiled bindings](https://learn.microsoft.com/en-us/dotnet/maui/fundamentals/data-binding/compiled-bindings?view=net-maui-10.0).

## Validasi

Properti R8 diverifikasi pada dokumentasi resmi dan targets SDK yang terpasang. Build, APK inspection, dan pengujian perangkat setelah perubahan dijalankan oleh alur QA utama. Ukur Release pada perangkat fisik; konfigurasi build saja tidak menggantikan perbaikan layout, render, dan gesture. [Microsoft: app performance](https://learn.microsoft.com/en-us/dotnet/maui/deployment/performance?view=net-maui-10.0).

Build final arm64 selesai dengan 0 warning dan 0 error. APK bertanda tangan berukuran
14.949.092 byte pada revisi pencarian/login/picker visual/AMOLED;
15.043.447 byte pada revisi katalog kantin/meja/indikator; revisi login/dialog/animasi
14.957.431 byte; revisi struk/notifikasi sebelumnya
16.420.383 byte; versi sebelum pembaruan UI/ikon/R8
berukuran 18.688.356 byte. Ini adalah perbandingan paket keseluruhan, bukan eksperimen
R8 terisolasi. Lisensi ikon ikut masuk APK, dan aturan keep JNI yang dihasilkan SDK
terbaca pada build final. Hasil uji HP dan frame time ada di [ui-validation.md](ui-validation.md).

Untuk satu ABI gunakan `RuntimeIdentifier` tunggal, termasuk saat memakai direktori
output pengujian terpisah dari Visual Studio:

```powershell
dotnet build KantinKita.csproj -c Release -t:SignAndroidPackage -f net10.0-android -p:RuntimeIdentifier=android-arm64
```
