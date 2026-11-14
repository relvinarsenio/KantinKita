# Pengelompokan di Visual Studio

`KantinKita.csproj` adalah proyek SDK .NET MAUI/C#. Visual Studio memakai folder
fisik seperti `Views`, `ViewModels`, `Services`, `Controls`, dan `Resources` sebagai
kategori di Solution Explorer. Tambahkan folder lewat **Add → New Folder**, lalu
pindahkan file terkait dan sesuaikan namespace/XAML jika namespace ikut berubah.

File `.csproj.filters` lama memakai format filter Visual C++. Microsoft
mendokumentasikan format tersebut sebagai **`.vcxproj.filters`**; mengganti awalan
menjadi `.csproj` tidak membuat sistem proyek C# memakai filter C++.
File yang tidak berlaku itu telah dihapus.

## Companion files di bawah XAML

`.filenesting.json` di root proyek menambahkan aturan yang didukung Visual Studio:

- `HomeView.xaml.cs` di bawah `HomeView.xaml` melalui aturan `addedExtension`.
- `HomeView.Android.cs` dan `HomeView.Notifications.Android.cs` di bawah XAML
  terkait melalui `fileSuffixToExtension`.
- Aturan yang sama berlaku bagi `MainTabsPage.Android.cs`.

Buka **`KantinKita.slnx`**, lalu di toolbar Solution Explorer aktifkan **File
Nesting → Default** (bukan Off). Bila proyek sudah terbuka saat konfigurasi
ditambahkan, reload proyek/solusi. File nesting .NET tersedia sejak VS 2022 17.3;
gunakan Visual Studio yang mendukung SDK/workload .NET 10 untuk proyek ini.

File nesting merapikan companion files, sedangkan kategori tingkat atas tetap
mengikuti folder proyek. Ini tidak mengubah build, namespace, atau lokasi file.

Sumber resmi Microsoft:

- [Vcxproj.filters files](https://learn.microsoft.com/en-us/cpp/build/reference/vcxproj-filters-files?view=msvc-170)
- [File nesting in Solution Explorer](https://learn.microsoft.com/en-us/visualstudio/ide/file-nesting-solution-explorer?view=visualstudio)
