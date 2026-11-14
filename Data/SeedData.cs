using KantinKita.Models;

namespace KantinKita.Data;

/// <summary>Static canteen catalogue. A real app would load this from an API.</summary>
public static class SeedData
{
    private static MenuOptionGroup SpiceLevels() => new(
        "Level pedas",
        [
            new("Tidak pedas"),
            new("Sedang"),
            new("Pedas"),
            new("Pedas banget", 1000m),
        ]);

    private static MenuOptionGroup Extras(params MenuOption[] extras) => new("Tambahan", extras);

    private static MenuOptionGroup DrinkSize(bool jumbo = true) => new(
        "Ukuran",
        jumbo ? [new("Reguler"), new("Jumbo", 3000m)] : [new("Reguler")]);

    private static MenuOptionGroup Sugar() => new(
        "Gula",
        [new("Normal"), new("Less sugar")]);

    private static MenuOptionGroup Ice() => new(
        "Es",
        [new("Es batu"), new("Tanpa es")]);

    private static IReadOnlyList<CanteenMenuItem> OriginalMenu { get; } =
    [
        new CanteenMenuItem
        {
            Id = "geprek",
            Name = "Nasi Ayam Geprek",
            Description = "Ayam crispy yang digeprek dengan sambal bawang pedas, disajikan dengan nasi hangat, lalapan, dan kerupuk.",
            Stall = "Warung Bu Sri",
            Category = MenuCategory.Makanan,
            Price = 15000m,
            TileStart = "#FDF0E4",
            TileEnd = "#F8DCC2",
            Rating = 4.9,
            ReviewCount = 412,
            PrepMinutes = 10,
            SoldCount = 1240,
            IsSpicy = true,
            IsBestSeller = true,
            Stock = 14,
            OptionGroups = [SpiceLevels(), Extras(new("Tanpa tambahan"), new("Telur ceplok", 3000m), new("Keju parut", 5000m), new("Nasi tambah", 4000m))],
        },
        new CanteenMenuItem
        {
            Id = "paket-hemat",
            Name = "Paket Hemat Siang",
            Description = "Nasi hangat, ayam goreng tepung, tumis sayur, dan es teh manis. Paket paling pas buat jam istirahat.",
            Stall = "Kantin Pusat",
            Category = MenuCategory.Makanan,
            Price = 18000m,
            TileStart = "#FDF1DC",
            TileEnd = "#F7D8A6",
            Rating = 4.8,
            ReviewCount = 356,
            PrepMinutes = 8,
            SoldCount = 1520,
            IsBestSeller = true,
            Stock = 25,
            OptionGroups =
            [
                new("Pilih lauk", [new("Ayam goreng"), new("Ikan kembung"), new("Tahu tempe")]),
                SpiceLevels(),
                new("Minuman", [new("Es teh manis"), new("Es jeruk", 2000m)]),
            ],
        },
        new CanteenMenuItem
        {
            Id = "mie-ayam",
            Name = "Mie Ayam Bakso",
            Description = "Mie kuning kenyal dengan ayam kecap, sawi hijau, dan bakso sapi. Kuah kaldu bening yang gurih.",
            Stall = "Mie Mas Joko",
            Category = MenuCategory.Makanan,
            Price = 13000m,
            TileStart = "#FFF3E2",
            TileEnd = "#FFE3BE",
            Rating = 4.8,
            ReviewCount = 286,
            PrepMinutes = 8,
            SoldCount = 980,
            IsBestSeller = true,
            Stock = 22,
            OptionGroups =
            [
                new("Porsi", [new("Reguler"), new("Jumbo", 4000m)]),
                Extras(new("Tanpa tambahan"), new("Bakso sapi", 5000m), new("Pangsit goreng", 4000m)),
            ],
        },
        new CanteenMenuItem
        {
            Id = "rendang",
            Name = "Nasi Rendang Daging",
            Description = "Rendang daging sapi empuk yang dimasak lama dengan rempah Minang, plus daun singkong dan sambal ijo.",
            Stall = "Dapur Minang",
            Category = MenuCategory.Makanan,
            Price = 18000m,
            TileStart = "#F8E9DF",
            TileEnd = "#EBC9B4",
            Rating = 4.9,
            ReviewCount = 198,
            PrepMinutes = 12,
            SoldCount = 640,
            Stock = 9,
            OptionGroups =
            [
                new("Porsi", [new("Reguler"), new("Porsi besar", 5000m)]),
                Extras(new("Tanpa tambahan"), new("Kerupuk merah", 2000m), new("Sambal ijo", 2000m)),
            ],
        },
        new CanteenMenuItem
        {
            Id = "nasi-goreng",
            Name = "Nasi Goreng Kampung",
            Description = "Nasi goreng dengan bumbu kampung, telur, dan sawi. Aroma smoky dari wok panas.",
            Stall = "Warung Bu Sri",
            Category = MenuCategory.Makanan,
            Price = 14000m,
            TileStart = "#FBF0DD",
            TileEnd = "#F3DDB6",
            Rating = 4.7,
            ReviewCount = 233,
            PrepMinutes = 9,
            SoldCount = 720,
            OptionGroups =
            [
                SpiceLevels(),
                Extras(new("Tanpa tambahan"), new("Telur mata sapi", 4000m), new("Sosis ayam", 5000m)),
            ],
        },
        new CanteenMenuItem
        {
            Id = "soto",
            Name = "Soto Ayam Lamongan",
            Description = "Kuah kuning bening dengan suwiran ayam, koya, telur, dan kentang goreng. Segar dan hangat.",
            Stall = "Soto Pak Karyo",
            Category = MenuCategory.Makanan,
            Price = 12000m,
            TileStart = "#FDF6DE",
            TileEnd = "#F6E6AA",
            Rating = 4.8,
            ReviewCount = 301,
            PrepMinutes = 6,
            SoldCount = 890,
            IsBestSeller = true,
            Stock = 18,
            OptionGroups =
            [
                new("Porsi", [new("Reguler"), new("Porsi besar", 4000m)]),
                Extras(new("Tanpa tambahan"), new("Telur rebus", 3000m), new("Kerupuk", 2000m)),
            ],
        },
        new CanteenMenuItem
        {
            Id = "gado-gado",
            Name = "Gado-Gado Siram",
            Description = "Sayuran segar rebus, lontong, tahu tempe, dan telur dengan saus kacang yang creamy.",
            Stall = "Saung Sehat",
            Category = MenuCategory.Sehat,
            Price = 12000m,
            TileStart = "#EAF7E6",
            TileEnd = "#CFEDC6",
            Rating = 4.6,
            ReviewCount = 143,
            PrepMinutes = 7,
            SoldCount = 410,
            Stock = 12,
            OptionGroups =
            [
                new("Saus", [new("Saus kacang"), new("Saus kacang pedas", 1000m)]),
                Extras(new("Tanpa tambahan"), new("Lontong", 3000m), new("Telur rebus", 3000m)),
            ],
        },
        new CanteenMenuItem
        {
            Id = "ayam-bakar",
            Name = "Ayam Bakar Madu",
            Description = "Paha ayam dibakar dengan olesan madu dan kecap, disajikan dengan nasi, lalapan, dan sambal terasi.",
            Stall = "Ayam Bakar Pak Ndut",
            Category = MenuCategory.Makanan,
            Price = 20000m,
            TileStart = "#FBEADF",
            TileEnd = "#F2C9AE",
            Rating = 4.8,
            ReviewCount = 176,
            PrepMinutes = 14,
            SoldCount = 520,
            IsSpicy = true,
            Stock = 7,
            OptionGroups =
            [
                new("Bumbu", [new("Manis"), new("Manis pedas")]),
                Extras(new("Tanpa tambahan"), new("Nasi putih", 4000m), new("Lalapan extra", 2000m)),
            ],
        },
        new CanteenMenuItem
        {
            Id = "es-teh",
            Name = "Es Teh Manis",
            Description = "Teh tubruk klasik dengan gula batu, disajikan dingin. Teman makan paling setia.",
            Stall = "Kantin Pusat",
            Category = MenuCategory.Minuman,
            Price = 4000m,
            TileStart = "#EAF2FB",
            TileEnd = "#D2E3F6",
            Rating = 4.7,
            ReviewCount = 864,
            PrepMinutes = 3,
            SoldCount = 2100,
            IsBestSeller = true,
            Stock = 40,
            OptionGroups = [DrinkSize(), Sugar(), Ice()],
        },
        new CanteenMenuItem
        {
            Id = "es-jeruk",
            Name = "Es Jeruk Peras",
            Description = "Jeruk peras segar tanpa sirup, dengan sedikit gula. Vitamin C untuk kelas berikutnya.",
            Stall = "Kantin Pusat",
            Category = MenuCategory.Minuman,
            Price = 6000m,
            TileStart = "#FFF1DE",
            TileEnd = "#FFDDAE",
            Rating = 4.8,
            ReviewCount = 322,
            PrepMinutes = 3,
            SoldCount = 780,
            Stock = 30,
            OptionGroups = [DrinkSize(), Sugar(), Ice()],
        },
        new CanteenMenuItem
        {
            Id = "kopi-aren",
            Name = "Kopi Susu Gula Aren",
            Description = "Espresso double shot dengan susu segar dan gula aren cair. Manis, creamy, bikin melek.",
            Stall = "Kopi Kampus",
            Category = MenuCategory.Minuman,
            Price = 12000m,
            TileStart = "#F1E7DF",
            TileEnd = "#DCC4B0",
            Rating = 4.9,
            ReviewCount = 508,
            PrepMinutes = 5,
            SoldCount = 1320,
            IsBestSeller = true,
            Stock = 25,
            OptionGroups =
            [
                new("Ukuran", [new("Reguler"), new("Large", 5000m)]),
                Sugar(),
                new("Suhu", [new("Dingin"), new("Panas")]),
            ],
        },
        new CanteenMenuItem
        {
            Id = "jus-almond",
            Name = "Jus Alpukat",
            Description = "Alpukat mentega pilihan, diblender dengan susu dan sedikit madu. Creamy dan mengenyangkan.",
            Stall = "Jus Segar Bu Tuti",
            Category = MenuCategory.Minuman,
            Price = 13000m,
            TileStart = "#EDF6E5",
            TileEnd = "#D2E9BE",
            Rating = 4.7,
            ReviewCount = 187,
            PrepMinutes = 6,
            SoldCount = 430,
            Stock = 10,
            OptionGroups =
            [
                new("Ukuran", [new("Reguler"), new("Jumbo", 4000m)]),
                new("Gula", [new("Normal"), new("Tanpa gula")]),
            ],
        },
        new CanteenMenuItem
        {
            Id = "pisang-keju",
            Name = "Pisang Goreng Keju",
            Description = "Pisang kepok goreng crispy dengan topping keju parut dan susu kental manis.",
            Stall = "Camilan Mbak Rini",
            Category = MenuCategory.Camilan,
            Price = 8000m,
            TileStart = "#FFF5DF",
            TileEnd = "#FBE3B0",
            Rating = 4.8,
            ReviewCount = 264,
            PrepMinutes = 5,
            SoldCount = 940,
            IsBestSeller = true,
            Stock = 16,
            OptionGroups =
            [
                new("Topping", [new("Keju"), new("Cokelat"), new("Keju cokelat", 2000m)]),
            ],
        },
        new CanteenMenuItem
        {
            Id = "tahu-crispy",
            Name = "Tahu Crispy Pedas",
            Description = "Tahu pong goreng garing dengan bumbu cabai kering. Camilan wajib saat nugas.",
            Stall = "Camilan Mbak Rini",
            Category = MenuCategory.Camilan,
            Price = 7000m,
            TileStart = "#FDF0E6",
            TileEnd = "#F7D6BD",
            Rating = 4.6,
            ReviewCount = 132,
            PrepMinutes = 4,
            SoldCount = 610,
            IsSpicy = true,
            Stock = 20,
            OptionGroups = [new("Level", [new("Original"), new("Pedas")])],
        },
        new CanteenMenuItem
        {
            Id = "roti-bakar",
            Name = "Roti Bakar Cokelat",
            Description = "Roti tawar tebal dibakar dengan mentega, isi cokelat lumer. Manis hangat.",
            Stall = "Camilan Mbak Rini",
            Category = MenuCategory.Camilan,
            Price = 9000m,
            TileStart = "#F8EDE3",
            TileEnd = "#E9D0B8",
            Rating = 4.5,
            ReviewCount = 98,
            PrepMinutes = 5,
            SoldCount = 350,
            Stock = 14,
            OptionGroups =
            [
                new("Isian", [new("Cokelat"), new("Keju"), new("Cokelat keju", 3000m)]),
            ],
        },
        new CanteenMenuItem
        {
            Id = "salad-buah",
            Name = "Salad Buah Yogurt",
            Description = "Potongan buah segar dengan yogurt plain dan granola. Pilihan ringan untuk siang hari.",
            Stall = "Saung Sehat",
            Category = MenuCategory.Sehat,
            Price = 15000m,
            TileStart = "#FDEFF3",
            TileEnd = "#F8D3DF",
            Rating = 4.7,
            ReviewCount = 76,
            PrepMinutes = 6,
            SoldCount = 210,
            Stock = 8,
            OptionGroups =
            [
                new("Saus", [new("Yogurt plain"), new("Yogurt stroberi", 2000m)]),
                Extras(new("Tanpa tambahan"), new("Granola", 3000m)),
            ],
        },
        new CanteenMenuItem
        {
            Id = "bubur-ayam",
            Name = "Bubur Ayam Komplit",
            Description = "Bubur lembut dengan ayam suwir, cakwe, telur, dan taburan bawang goreng.",
            Stall = "Soto Pak Karyo",
            Category = MenuCategory.Makanan,
            Price = 11000m,
            TileStart = "#FFF8E8",
            TileEnd = "#F4E4B7",
            Rating = 4.6,
            ReviewCount = 154,
            PrepMinutes = 7,
            SoldCount = 470,
            Stock = 13,
            OptionGroups =
            [
                Extras(new("Tanpa tambahan"), new("Cakwe", 2000m), new("Telur rebus", 3000m)),
            ],
        },
    ];

    public static IReadOnlyList<CanteenMenuItem> Menu { get; } = BuildMenus();

    private static IReadOnlyList<CanteenMenuItem> BuildMenus()
    {
        var menus = new List<CanteenMenuItem>(OriginalMenu);
        AddOffers("kantin-b", ["geprek", "nasi-goreng", "soto", "ayam-bakar", "es-teh", "kopi-aren", "tahu-crispy", "pisang-keju", "paket-hemat"], 1000m);
        AddOffers("kantin-c", ["mie-ayam", "gado-gado", "bubur-ayam", "salad-buah", "jus-almond", "es-teh", "roti-bakar", "paket-hemat"], 500m);
        return menus;

        void AddOffers(string canteenId, string[] dishIds, decimal priceDifference)
        {
            foreach (var dishId in dishIds)
            {
                var source = OriginalMenu.Single(item => item.Id == dishId);
                menus.Add(new CanteenMenuItem
                {
                    Id = $"{canteenId}:{source.Id}", CanteenId = canteenId,
                    Name = source.Name, Description = source.Description, Stall = source.Stall,
                    Category = source.Category, Price = source.Price + priceDifference,
                    TileStart = source.TileStart, TileEnd = source.TileEnd,
                    Rating = source.Rating, ReviewCount = source.ReviewCount,
                    PrepMinutes = source.PrepMinutes, SoldCount = source.SoldCount,
                    Stock = source.Stock, IsSpicy = source.IsSpicy, IsBestSeller = source.IsBestSeller,
                    OptionGroups = source.OptionGroups,
                });
            }
        }
    }

    public static IReadOnlyList<Voucher> Vouchers { get; } =
    [
        new("HEMAT5", "Potongan Rp5.000", "Min. belanja Rp25.000", 25000m, FlatDiscount: 5000m),
        new("MAHASISWA10", "Diskon 10%", "Min. belanja Rp30.000 · maks. Rp15.000", 30000m, Percent: 10m, MaxDiscount: 15000m),
        new("NGOPI3", "Potongan Rp3.000", "Min. belanja Rp15.000", 15000m, FlatDiscount: 3000m),
    ];
}

