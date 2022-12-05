using SB_Parser_API.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using static SB_Parser_API.MicroServices.Utils;

namespace SB_Parser_API.MicroServices
{
    public static class DBSerices
    {
        public enum DBParam : int
        {
            TotalPriceScanners = 2, HideMyNameProxy = 11
        }

        static object Lproxy = new object();
        static object LdeadProxy = new object();
        static object LproxyToDomain = new object();

        //Microsoft.EntityFrameworkCore.Internal.InternalDbSet`1[[SB_Parser_API.Models.Retailer, SB_Parser_API, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]
        public static Dictionary<string, string> DB_Entity_Relation = new Dictionary<string, string>();
        static Regex getFName = new Regex(@"[^[]+\[+([^,]+),.+");//\[\[  [[]{2}
        static Regex getName = new Regex(@".+\.([^\.]+)");
        public static void InitDBSerices()
        {
            DbContext db = null!;

            var fill_DB_Entity_Relation = (string dbn) =>
            {
                var dbSets = db.GetType().GetProperties().Select(x => x.GetValue(db)).Where(x => x!.GetType().FullName!.Contains("InternalDbSet"))!;
                foreach (var dbs in dbSets)
                {
                    string fnl = dbs?.GetType().FullName ?? "";
                    string fn = getFName.Replace(fnl, "$1");
                    string sn = getName.Replace(fn, "$1");
                    DB_Entity_Relation.TryAdd(fn, dbn);
                    DB_Entity_Relation.TryAdd(sn, dbn);
                }
            };

            DB_Entity_Relation.Clear();

            db = new PISBContext();
            fill_DB_Entity_Relation("PISB");
            db.Dispose();

            db = new PIPContext();
            fill_DB_Entity_Relation("PIP");
            db.Dispose();
        }
        public static DbContext GetDBContext(Type eType)
        {
            var dbn = DB_Entity_Relation[eType.Name];
            switch(dbn)
            {
                case "PISB":
                    return new PISBContext();
                case "PIP":
                    return new PIPContext();
            }
            return null!;
        }
        public static string? GetDBName(Type eType)
        {
            return DB_Entity_Relation[eType.Name];
        }
        public static List<Proxy> GetProxyDB_List()
        {
            if (GetDBName(typeof(Proxy)) != "PIP")
                return null!;
            using var db = new PIPContext();
            return db.Proxies.ToList();
        }
        public static List<Proxy> GetProxyDB_partList()
        {
            if (GetDBName(typeof(Proxy)) != "PIP")
                return null!;
            using var db = new PIPContext();
            return db.Proxies.Select(x => (new Proxy() { id = x.id, ip = x.ip, port = x.port, protocol = x.protocol })).ToList();
        }
        public static List<DeadProxy> GetDeadProxyDB_List()
        {
            if (GetDBName(typeof(DeadProxy)) != "PIP")
                return null!;
            using var db = new PIPContext();
            return db.DeadProxies.ToList();
        }
        public static List<DeadProxy> GetDeadProxyDB_partList()
        {
            if (GetDBName(typeof(DeadProxy)) != "PIP")
                return null!;
            using var db = new PIPContext();
            return db.DeadProxies.Select(x => (new DeadProxy() { id = x.id, ip = x.ip, port = x.port, protocol = x.protocol })).ToList();
        }
        public static int? IsInProxyDB(Proxy p)
        {
            if (GetDBName(p.GetType()) != "PIP")
                return null;
            using var db = new PIPContext();
            var dp = db.Proxies.FirstOrDefault(x => x.ip == p.ip && x.port == p.port && x.protocol == p.protocol);
            return dp?.id;
        }
        public static int? IsInDeadProxyDB(Proxy p)
        {
            if (GetDBName(p.GetType()) != "PIP")
                return null;
            using var db = new PIPContext();
            var dp = db.DeadProxies.FirstOrDefault(x => x.ip == p.ip && x.port == p.port && x.protocol == p.protocol);
            return dp?.id;
        }

        public static int? ProxyAddNew(Proxy pr)
        {
            while (true)
            {
                try
                {
                    if (GetDBName(pr.GetType()) != "PIP")
                        return null;
                    using var db = new PIPContext();
                    lock (Lproxy)
                    {
                        db.DeadProxies.RemoveRange(db.DeadProxies.Where(x => x.ip == pr.ip && x.port == pr.port && x.protocol == pr.protocol));
                        db.Proxies.RemoveRange(db.Proxies.Where(x => x.ip == pr.ip && x.port == pr.port && x.protocol == pr.protocol));
                        pr.id = getIDforNewRecord(db.Proxies.Select(x => x.id).ToList());
                        db.Proxies.Add(pr);
                        db.SaveChanges();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"DB ERROR_Proxy!!!! -- {ex.Message}");
                    continue;
                }
                break;
            }
            return pr.id;
        }
        public static int? DeadProxyAddNew(DeadProxy pr)
        {
            while (true)
            {
                try
                {
                    if (GetDBName(pr.GetType()) != "PIP")
                        return null;
                    using var db = new PIPContext();
                    lock (LdeadProxy)
                    {
                        db.DeadProxies.RemoveRange(db.DeadProxies.Where(x => x.ip == pr.ip && x.port == pr.port && x.protocol == pr.protocol));
                        db.Proxies.RemoveRange(db.Proxies.Where(x => x.ip == pr.ip && x.port == pr.port && x.protocol == pr.protocol));
                        pr.id = getIDforNewRecord(db.DeadProxies.Select(x => x.id).ToList());
                        db.DeadProxies.Add(pr);
                        db.SaveChanges();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"DB ERROR_DeadProxy!!!! -- {ex.Message}");
                    continue;
                }
                break;
            }
            return pr.id;
        }
        public static List<Proxy_to_Domain> ProxyToDomainGet(Proxy_to_Domain ptd = null!)
        {
            ptd ??= new Proxy_to_Domain();
            while (true)
            {
                try
                {
                    if (GetDBName(ptd!.GetType()) != "PIP")
                        return new List<Proxy_to_Domain>();
                    using var db = new PIPContext();
                    if (ptd.ip.Length > 0)
                        return db.Proxy_to_Domain.Where(x => x.ip == ptd.ip && x.port == ptd.port && x.protocol == ptd.protocol &&
                        (ptd.domain.Length == 0 ? true : x.domain == ptd.domain)).ToList();
                    else
                        return db.Proxy_to_Domain.ToList();

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"DB ERROR_ProxyToDomain (GET) !!!! -- {ex.Message}");
                    Task.Delay(1000).Wait();
                    continue;
                }
            }
        }
        public static void ProxyToDomainAddOrUpdate(Proxy_to_Domain ptd)
        {
            while (true)
            {
                try
                {
                    if (GetDBName(ptd.GetType()) != "PIP")
                        return;
                    using var db = new PIPContext();
                    lock (LproxyToDomain)
                    {
                        db.Proxy_to_Domain.RemoveRange(db.Proxy_to_Domain.Where(x => x.ip == ptd.ip && 
                        x.port == ptd.port && x.protocol == ptd.protocol && x.domain == ptd.domain).ToArray());
                        //var ptdInDB = db.Proxy_to_Domain.FirstOrDefault(x => x.ip == ptd.ip && x.port == ptd.port && x.protocol == ptd.protocol && x.domain == ptd.domain);
                        //if (ptdInDB is not null)
                        //    db.Proxy_to_Domain.Remove(ptdInDB);
                        db.Proxy_to_Domain.Add(ptd);
                        db.SaveChanges();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"DB ERROR_ProxyToDomain!!!! -- {ex.Message}");
                    Task.Delay(1000).Wait();
                    continue;
                }
                break;
            }
            return;
        }
        public static (int?, string) GetDBParam(DBParam par)
        {
            PI_Parameter? p;
            if (GetDBName(typeof(PI_Parameter)) != "PIP")
                return (0,"");
            using var db = new PIPContext();
            p = db.PI_Parameters.FirstOrDefault(x => x.id == (int)par);
            return (p!.value ?? 0,p.valueS ?? "");
        }
    }
}
