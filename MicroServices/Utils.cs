using System.Text.RegularExpressions;
using System.Web;

namespace SB_Parser_API.MicroServices
{
    public static class Utils
    {
        public static int getIDforNewRecord(List<int> ids)
        {
            int i;
            ids.Sort();
            for (i = 1; (i <= ids.Count()) && ((i >= ids[i - 1])); i++) ;
            return (i);
        }
        public static string Get_DB_ConnectionString(string DB_Name)
        {
            string CS_Name = "PIDB";
            const int sk = 125;const int sk1 = 664;
            var builder = new ConfigurationBuilder();
            builder.SetBasePath(Directory.GetCurrentDirectory());
            builder.AddJsonFile("Properties/launchSettings.json"); //appsettings.json
            return builder.Build().GetConnectionString(CS_Name)!.Replace(CS_Name, DB_Name).Replace($"gh{sk}",$"gh{sk+sk1}");
        }
        public static double CalcDistance(double lat1, double lon1, double lat2, double lon2)
        {
            Func<double, double> Radians = (angle) => angle * Math.PI / 180.0;
            const double earthRadius = 6371;
            double delataSigma = Math.Acos(Math.Sin(Radians(lat1)) * Math.Sin(Radians(lat2)) +
                Math.Cos(Radians(lat1)) * Math.Cos(Radians(lat2)) * Math.Cos(Math.Abs(Radians(lon2) - Radians(lon1))));
            return earthRadius * delataSigma;
        }
        static string normStr(string s)
        {
            s = HttpUtility.HtmlDecode(s);
            s = s.Replace(";", ",");
            s = s.Replace("\"", "'");
            return (s);
        }
        static public string BarCodeCheck(string barOrig)
        {
            Regex rg_cut, takeD, cutSZ;
            int ksum13, len, ks, ksum, ib, lsum;
            string ls;
            takeD = new Regex(@"\D", RegexOptions.IgnoreCase); // не цифры
            cutSZ = new Regex(@"^0+", RegexOptions.IgnoreCase); // передние нули
            rg_cut = new Regex(@"^\d{8,14}$", RegexOptions.IgnoreCase); // цифры
            if (barOrig is null || barOrig == "")
                return ("");
            ls = takeD.Replace(barOrig, "");
            ls = cutSZ.Replace(ls, "");

            ksum13 = 0;
            if (rg_cut.IsMatch(ls))
            {
                ls = rg_cut.Match(ls).ToString();
                len = ls.Length;
                for (ib = len - 2, ks = 2, ksum = 0; ib >= 0; ib--)
                {
                    lsum = (int)ls[ib] - 48;//'0'=48
                    ksum += lsum + lsum * ks;
                    if (ks == 2)
                        ks = 0;
                    else
                        ks = 2;
                    if ((ib == 1) && (len == 14))
                        ksum13 = ksum;
                }
                ksum = (10 - ksum % 10) % 10;
                lsum = (int)(ls[len - 1]) - 48;//'0'=48
                if (len == 14)
                {
                    if (ls[0] == '1')
                    {
                        ksum13 = (10 - ksum13 % 10) % 10;
                        ls = ls.Substring(1, 12) + ((char)(ksum13 + 48)).ToString(); //'0'=48
                        return (cutSZ.Replace(ls, ""));
                    }
                    if (BarCodeCheck(ls.Substring(0, 13)) == "")
                        return (BarCodeCheck(ls.Substring(1, 13)));
                    return (BarCodeCheck(ls.Substring(0, 13)));
                }
                if (lsum != ksum)
                    return ("");
                return (cutSZ.Replace(ls, ""));
                //list.Add(ifooods.Rows[i]["ID"].ToString() + ";" + ls + ";" + ifooods.Rows[i]["Bar_Orig"].ToString() + ";" + len.ToString());// + ";" + ifooods.Rows[i]["Name"].ToString());
            }
            return ("");
        }
    }
}
