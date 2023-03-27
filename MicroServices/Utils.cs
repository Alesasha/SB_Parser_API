using System.Text.RegularExpressions;
using System.Web;
using SB_Parser_API.Models;
using static SB_Parser_API.MicroServices.SearchViaBarcode;

namespace SB_Parser_API.MicroServices
{
    public static class Utils
    {
        public static IEnumerable<long> RangeL(long first, long last)
        {
            for (long i = first; i <= last; i++)
            {
                yield return i;
            }
        }
        public static int getIDforNewRecord(List<int>? ids)
        {
            int i;
            if (ids is null || ids.Count() == 0)
                return (1);
            ids.Sort();
            for (i = 1; (i <= ids.Count()) && ((i >= ids[i - 1])); i++) ;
            return (i);
        }
        public static List<int> GetFreeIdList(List<int> idDBL)
        {
            if (idDBL.Count <= 0) return new List<int>() { 1 };
            return Enumerable.Range(1, idDBL.Max() + 1).Except(idDBL).OrderDescending().ToList();
        }
        public static List<long> GetFreeIdList(List<long> idDBL)
        {
            if (idDBL.Count <= 0) return new List<long>() { 1 };
            return RangeL(1,idDBL.Max() + 1).Except(idDBL).OrderDescending().ToList();
        }
        public static int getIDforNewRecordFromFreeIdList(List<int> fids)
        {
            var i = fids.Last();
            fids.Remove(i);
            if (fids.Count <= 0) fids.Add(i + 1);
            return (i);
        }
        public static long getIDforNewRecordFromFreeIdList(List<long> fids)
        {
            var i = fids.Last();
            fids.Remove(i);
            if (fids.Count <= 0) fids.Add(i + 1);
            return (i);
        }
        public static void addFreeIdToList(List<int> fids, int fid)
        {
            fids.Add(fid);
            var fidsL = fids.Distinct().OrderDescending().ToList();
            fids.Clear();
            for (int i = 0; i < fidsL.Count; i++)
                fids.Add(fidsL[i]);
        }
        public static void addFreeIdsToList(List<int> fids, List<int> nfids)
        {
            fids.AddRange(nfids);
            var fidsL = fids.Distinct().OrderDescending().ToList();
            fids.Clear();
            for (int i = 0; i < fidsL.Count; i++)
                fids.Add(fidsL[i]);
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

        static public List<string>? BarCodeCheckList(List<string>? barLst)
        {
            if (barLst is null) return null;
            for (var i = 0; i < barLst.Count; i++)
                barLst[i] = BarCodeCheck(barLst[i]);
            return barLst.Distinct().Where(x=>x.Length>0).ToList();
        }
        static public string BarCodeCheck(string barOrig)
        {
            Regex rg_cut, takeD, cutSZ;
            int ksum13, len, ks, ksum, ib, lsum;
            string ls;
            takeD = new Regex(@"\D", RegexOptions.IgnoreCase); // не цифры
            cutSZ = new Regex(@"^0+", RegexOptions.IgnoreCase); // передние нули
            rg_cut = new Regex(@"^\d{6,14}$", RegexOptions.IgnoreCase); // цифры
            if (barOrig is null || barOrig == "")
                return ("");
            ls = takeD.Replace(barOrig, "");
            ls = cutSZ.Replace(ls, "");

            ksum13 = 0;
            if (rg_cut.IsMatch(ls))
            {
                if (ls[0] == '3' && ls.Length == 7) // Это для кодов SB по Вкус-Вилл
                    return ls;

                ls = rg_cut.Match(ls).ToString();
                len = ls.Length;
                for (ib = len - 2, ks = 2, ksum = 0; ib >= 0; ib--)
                {
                    lsum = (int)ls[ib] - 48; //'0'=48
                    ksum += lsum + lsum * ks;
                    ks = ks == 2 ? 0 : 2;
                    /*
                    if (ks == 2)
                        ks = 0;
                    else
                        ks = 2;
                    */
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

        /*
	Набор программ для нечёткого сравнения похожести строк. 

Варианты использование:

Вариант 1. Как раньше медленно, но точнее
	int res = CompResultFine(string st1, string st2);

Вариант 2. Быстрый, с предварительным отсевом
	int res = CompResultFast(string st1, string st2);

Вариант 3. Ещё Быстрее, для сравнения с предустановленным образцом и предварительным отсевом	
	SetSample(string st1); // установка образца
	int res = CompResultSample(string st2); // сравнение с предустановленым образцом


	Набор программ для формирования отсортированных по рейтингу массивов с результатами.

Использование

		void InitResArrays(int len);  
			Инициализирует массивы для хранения лучших результатов 
			len - количество лучших значений, которые надо отобрать и отсортировать по рейтингу.

		Boolean TryInsertCurrentResult(int res,int ind)
			Пытается вставить текущую строку в топ лист. Если подошла - вернёт true, если нет - false
			res - рейтинг строки, ind - ID (или индекс строки)

	        int[] GetResultArrayRating()
			Возвращает отсортированный массив рейтингов

	        int[] GetResultArrayIndex()
			Возвращает отсортированный массив индексов по рейтингу
*/

        //___________________________________________________________________
        static int[] BRB_res= { };
        static int[] BRB_ind= { };
        static int res_pr;
        static Object insertLock = new Object();

        static public void InitResArrays(int len)
        {
            BRB_res = new int[len];
            BRB_ind = new int[len];
            res_pr = 0;
        }

        static public Boolean TryInsertCurrentResult(int res, int ind)
        {
            int i, j, len, restmp, indtmp;

            lock (insertLock)
            {
                if (res > res_pr)
                {
                    BRB_res[0] = res;
                    BRB_ind[0] = ind;
                    len = BRB_res.Length;
                    for (i = 0; i < len; i++)
                    {
                        for (j = i; j < len; j++)
                        {
                            if (BRB_res[i] > BRB_res[j])
                            {
                                restmp = BRB_res[i];
                                BRB_res[i] = BRB_res[j];
                                BRB_res[j] = restmp;
                                indtmp = BRB_ind[i];
                                BRB_ind[i] = BRB_ind[j];
                                BRB_ind[j] = indtmp;
                            }
                        }
                    }
                    res_pr = BRB_res[0];
                    return (true);
                }
                return (false);
            }
        }
        static public int[] GetResultArrayRating()
        {
            return (BRB_res);
        }
        static public int[] GetResultArrayIndex()
        {
            return (BRB_ind);
        }


        //___________________________________________________________________
        public class FindRelevantNames
        {
            public List<ProductName> TopNames = new();
            public ProductName minRateItem = null!;
            public static string[] sfcWords = { };
            public static string sampleFC = "";
            public static int topResultCount = 30;
            public void SetSample(string st, int? trc)
            {
                topResultCount = trc ?? topResultCount;
                st = PreProcessString(st);
                sfcWords = st.Split(' ').Where(x => x.Length > 2).ToArray(); // строка в слова
                sampleFC = st;
            }
            public void FindBestProductNames(ProductName pn)
            {
                var curRate = CompResultSample(sampleFC, pn.namepp, sfcWords);
                int minRate;
                lock (TopNames)
                {
                    if (TopNames.Count < topResultCount)
                    {
                        pn.rate = curRate;
                        TopNames.Add(pn);
                        minRate = TopNames.Select(x => x.rate).Min();
                        minRateItem = TopNames.FirstOrDefault(x => x.rate == minRate)!;
                        return;
                    }
                    if (curRate > minRateItem.rate)
                    {
                        TopNames.Remove(minRateItem);
                        pn.rate = curRate;
                        TopNames.Add(pn);
                        minRate = TopNames.Select(x => x.rate).Min();
                        minRateItem = TopNames.FirstOrDefault(x => x.rate == minRate)!;
                    }
                }
            }
        }

        //___________________________________________________________________
        static string[] SFCWords = { };
        static string SampleFC="";

        static public string PreProcessString(string st)
        {
            while (st.Length != (st = st.Replace("  ", " ")).Length) ; // Подавление лишних пробелов.
            st = st.ToLower();
            st = st.Replace("ё", "е"); // Подавление ё.
            st = st.Replace(",", "."); // Подавление разницы между ',' и '.'.
            return (st);
        }
        static public int CompResultFast(string st1, string st2)
        {
            string SampleFCSafe, st0;
            string[] SFCWordsSafe;
            int l0, l1, l2, res;

            SampleFCSafe = SampleFC;
            SFCWordsSafe = SFCWords;

            l1 = st1.Length;
            l2 = st2.Length;
            if (l1 > l2)
            {
                st0 = st1;
                l0 = l1;
                st1 = st2;
                l1 = l2;
                st2 = st0;
                l2 = l0;
            }
            SetSample(st1);
            res = CompResultSample(st2);
            SampleFC = SampleFCSafe;
            SFCWords = SFCWordsSafe;
            return (res);

        }
        static public void SetSample(string st)
        {
            st = PreProcessString(st);
            SFCWords = st.Split(' ').Where(x => x.Length > 2).ToArray(); // строка в слова
            SampleFC = st;
        }
        static public int CompResultSample(string st2)
        {
            string st1;
            int sw;

            //st2 = PreProcessString(st2);

            sw = 0;
            st1 = SampleFC;
            foreach (string sts in SFCWords)
            {
                if (st2.Contains(sts)) //IndexOf ((sts.Length > 2) && (st2.Length >= sts.Length ? st2.Contains(sts) : sts.Contains(st2))) //IndexOf
                {
                    sw = 1;
                    break;
                }
            }
            if (sw == 0)
                return (0);
            return (CompResultSys(st1, st2));
        }
        static public int CompResultSample(string st1, string st2, string[] sfcw)
        {
            int sw;
            //st2 = PreProcessString(st2);
            sw = 0;
            foreach (string sts in sfcw)
            {
                if (st2.Contains(sts)) //IndexOf ((sts.Length > 2) && (st2.Length >= sts.Length ? st2.Contains(sts) : sts.Contains(st2))) //IndexOf
                {
                    sw = 1;
                    break;
                }
            }
            if (sw == 0)
                return (0);
            return (CompResultSys(st1, st2));
        }
        static public int CompResultFine(string st1, string st2)
        {
            st1 = PreProcessString(st1);
            st2 = PreProcessString(st2);
            return (CompResultSys(st1, st2));
        }
        static int CompResultSys(string st1, string st2)
        {
            string st0;
            int i, j, k1, k2, l0, l1, l2, lmin, lminc, lm, lmm, mltot, ml, mp, mc, mpp, mlp;

            lmin = 3;
            l1 = st1.Length;
            l2 = st2.Length;
            if (l1 > l2)
            {
                st0 = st1;
                l0 = l1;
                st1 = st2;
                l1 = l2;
                st2 = st0;
                l2 = l0;
            }
            if (l1 < lmin) lmin = l1;
            if (l1 == 0)
            {
                if (l2 == 0)
                    return (10100);
                else
                    return (0);
            }
            mpp = 0;
            mlp = 0;
            for (i = 0, mp = 0, ml = 0, mc = 0, mltot = 0; i <= l1 - lmin; i++)
            {
                for (j = 0, lmm = 0, lminc = lmin; j <= l2 - lminc; j++)
                {
                    for (k1 = i, k2 = j, lm = 0; (k1 < l1) && (k2 < l2); k1++, k2++)
                    {
                        if (st1[k1] == st2[k2])
                            lm++;
                        else
                            break;
                    }

                    if (lm > lmm)
                        lmm = lm;
                    if (lminc < lmm)
                        lminc = lmm;
                }
                if (lmm < lmin)
                    continue;
                mc++;
                mltot += lmm;
                if (mp + ml > i)
                {
                    if (i + lmm > mp + ml)
                    {
                        if (mp > mpp)
                        {
                            mc--;
                            mltot -= ml;
                            mp = mpp;
                            ml = mlp;
                        }
                    }
                    else
                    {
                        mc--;
                        mltot -= lmm;
                        continue;
                    }
                    if (lmm > ml)
                    {
                        mltot -= ml;
                        if (i - mp < lmin)
                            mc--;
                        else
                            mltot += i - mp;
                        mp = i;
                        mpp = i;
                        ml = lmm;
                        mlp = lmm;
                    }
                    else
                    {
                        if (i + lmm - mp - ml < lmin)
                        {
                            mc--;
                            mltot -= lmm;
                        }
                        else
                        {
                            mltot -= mp + ml - i;
                            mp += ml;
                            ml = i + lmm - mp;
                        }
                    }
                }
                else
                {
                    mp = i;
                    ml = lmm;
                    mpp = i;
                    mlp = lmm;
                }
            }
            if (mltot > 0)
                return ((mltot * 100 / l1) * 100 + 140 - mc * 8 - l2 * 32 / l1);
            else
                return (0);
        }
    }
}
    

