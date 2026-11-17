using HCSTT.Fuzzy;

// ======== BÀI TẬP BUỔI 1 - 10/08/2026 ==========
// BÀI TOÁN: Xe máy giới hạn vận tốc 0..100 km/h
// Xây 3 tập mờ ngôn ngữ: Thấp (Low), Trung bình (Medium), Cao (High)
// Rời rạc hoá theo mốc 0,10,20,...,90,100

// static double ShoulderLeft(double x, double a, double b)
// {
//     if (x <= a) return 1.0;
//     if (x >= b) return 0.0;
//     return (b - x) / (b - a);
// }

// static double ShoulderRight(double x, double a, double b)
// {
//     if (x <= a) return 0.0;
//     if (x >= b) return 1.0;
//     return (x - a) / (b - a);
// }

// static double Trimf(double x, double a, double b, double c, double epsilon = 1e-6)
// {
//     if (x <= a || x >= c) return 0.0;
//     if (Math.Abs(x - b) < epsilon) return 1.0;
//     if (x < b) return (x - a) / (b - a);
//     return (c - x) / (c - b);
// }

// // Vũ trụ: vận tốc rời rạc hóa: 0, 10, 20, ..., 100
// var speedMarks = Enumerable.Range(0, 11).Select(i => i * 10).ToList();
// var speedU = new Universe<int>(speedMarks);
// Console.WriteLine("Vũ trụ U = " + speedU);
// Console.WriteLine();

// // 3 tập mờ
// var low = new FuzzySet<int>(speedU, "Thấp");
// var medium = new FuzzySet<int>(speedU, "Trung bình");
// var high = new FuzzySet<int>(speedU, "Cao");

// foreach (var x in speedU.GetAllElements())
// {
//     low[x] = ShoulderLeft(x, 20, 40);
//     medium[x] = Trimf(x, 30, 50, 70);
//     high[x] = ShoulderRight(x, 60, 80);
// }

// // In
// Console.WriteLine(low);
// Console.WriteLine(medium);
// Console.WriteLine(high);
// Console.WriteLine();

// ======== BÀI TẬP BUỔI 2 - 14/08/2026 ==========
// var u = new Universe<string>(["a", "b", "c", "d"]);
// var v = new Universe<string>(["m", "l", "p"]);

// var a = new FuzzySet<string>(u, "A")
// {
//     ["a"] = 0.3,
//     ["b"] = 0.7,
//     ["c"] = 0.9,
//     ["d"] = 0.4,
// };

// var b = new FuzzySet<string>(v, "B")
// {
//     ["m"] = 0.1,
//     ["l"] = 0.8,
//     ["p"] = 0.6,
// };

// Console.WriteLine("Cho U = " + u + ", V = " + v);
// Console.WriteLine(a);
// Console.WriteLine(b);
// Console.WriteLine();

// Console.WriteLine("===== BAI 1: Tinh M = A x B; N = B x A =====");
// var m = FuzzySet<string>.CartesianProduct(a, b);
// var n = FuzzySet<string>.CartesianProduct(b, a);
// Console.WriteLine(m);
// Console.WriteLine(n);

// Console.WriteLine("===== BAI 2: Hinh chieu cua M va N =====");
// var projMOnU = FuzzySet<string>.ProjectionOnFirst(m);
// var projMOnV = FuzzySet<string>.ProjectionOnSecond(m);
// Console.WriteLine("Hinh chieu cua M tren U: " + projMOnU);
// Console.WriteLine("Hinh chieu cua M tren V: " + projMOnV);

// var projNOnV = FuzzySet<string>.ProjectionOnFirst(n);
// var projNOnU = FuzzySet<string>.ProjectionOnSecond(n);
// Console.WriteLine("Hinh chieu cua N tren U: " + projNOnU);
// Console.WriteLine("Hinh chieu cua N tren V: " + projNOnV);
// Console.WriteLine();

// Console.WriteLine("===== BAI 3: Mo rong hinh tru tren U x V =====");
// var ceA = FuzzySet<string>.CylindricalExtensionFrom1(a, v);
// var ceB = FuzzySet<string>.CylindricalExtensionFrom2(u, b);
// Console.WriteLine("Mo rong hinh tru cua A tren U x V:");
// Console.WriteLine(ceA);
// Console.WriteLine("Mo rong hinh tru cua B tren U x V:");
// Console.WriteLine(ceB);


var U = new Universe<string>(["a", "b", "c", "d"]);
var V = new Universe<string>(["m", "l", "p"]);
var W = new Universe<string>(["x", "y", "z"]);

var R = new FuzzyRelation<string, string>(U, V, "R");
R["a", "m"] = 0.3; R["a", "l"] = 0.7; R["a", "p"] = 0.4;
R["b", "m"] = 0.9; R["b", "l"] = 0.6; R["b", "p"] = 0.2;
R["c", "m"] = 0.5; R["c", "l"] = 0.1; R["c", "p"] = 0.8;
R["d", "m"] = 0.6; R["d", "l"] = 0.8; R["d", "p"] = 0.0;

var S = new FuzzyRelation<string, string>(V, W, "S");
S["m", "x"] = 0.6; S["m", "y"] = 0.1; S["m", "z"] = 0.7;
S["l", "x"] = 0.2; S["l", "y"] = 0.5; S["l", "z"] = 0.9;
S["p", "x"] = 0.8; S["p", "y"] = 0.3; S["p", "z"] = 0.8;

Console.WriteLine(R);
Console.WriteLine(S);

// Câu a: Max-Min
var maxMinStrategy = new MaxMinComposition();
var resultMaxMin = R.ComposeWith(S, maxMinStrategy);
Console.WriteLine(resultMaxMin);

// Câu b: Max-Product
var maxProdStrategy = new MaxProductComposition();
var resultMaxProd = R.ComposeWith(S, maxProdStrategy);
Console.WriteLine(resultMaxProd);

// Câu e: Đề xuất Max-Lukasiewicz (Chỉ cần truyền Strategy mới)
var maxLukaStrategy = new MaxLukasiewiczComposition();
var resultLuka = R.ComposeWith(S, maxLukaStrategy);
Console.WriteLine(resultLuka);