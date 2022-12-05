using System.ComponentModel.DataAnnotations.Schema;
namespace SB_Parser_API.Models
{
    //_________________________Reverse Engineering Models______________________________________//

/*
Scaffold-DbContext "Data Source=192.168.1.250;Initial Catalog=PIP;User ID=saf;Password=a21sdgh789As;Connection Timeout=360;Encrypt=False;" Microsoft.EntityFrameworkCore.SqlServer -OutputDir Models -Tables "Proxy","DeadProxy","IG_Proxy_Token","IG_Proxy_Token_Archive" -ContextDir Models -UseDatabaseNames
Scaffold-DbContext "Data Source=192.168.1.250;Initial Catalog=PIP;User ID=saf;Password=a21sdgh789As;Connection Timeout=360;TrustServerCertificate=true;" Microsoft.EntityFrameworkCore.SqlServer -OutputDir Models -Tables "Proxy","DeadProxy","IG_Proxy_Token","IG_Proxy_Token_Archive" -ContextDir Models -UseDatabaseNames
dotnet user-secrets init
Set UserSecretsId to '93e1aff3-a1b8-413d-90ea-867fe0579456' for MSBuild project 'C:\Users\Alexander\source\repos\SB_Parser_API\SB_Parser_API.csproj'.
dotnet user-secrets set ConnectionStrings:PISB "Data Source=192.168.1.251;Initial Catalog=PISB;User ID=sa;Password=asdgh789As;Connection Timeout=360;TrustServerCertificate=true;"
Successfully saved ConnectionStrings:PISB = Data Source = 192.168.1.251;Initial Catalog = PISB; User ID = sa; Password=asdgh789As;Connection Timeout = 360; TrustServerCertificate=true; to the secret store.
*/

    //_________________________PIP_DB Models______________________________________//

    //Proxy model
    public class Proxy
    {
        public int id { get; set; }
        public string? ip { get; set; }
        public string? port { get; set; }
        public string? location { get; set; }
        public int? delay { get; set; }
        public string? protocol { get; set; }
        public string? defence { get; set; }
        public DateTime? lastCheck { get; set; }
        public DateTime? insertDateTime { get; set; }
        public int? rate { get; set; }
        public int? tested { get; set; }
        public int? testOK { get; set; }
        public int? lastRate { get; set; }
        public string? login { get; set; }
        public string? pass { get; set; }
        public bool? status { get; set; }
        public DateTime? pauseTill { get; set; }
        public bool? inUse { get; set; }
        public int? inUseBy { get; set; }
        [NotMapped]
        public string delayS = "";
        [NotMapped]
        public int delayms;
        [NotMapped]
        public string lastCheckS = "";
        [NotMapped]
        public string Speed = "";
        [NotMapped]
        public string UpTime = "";
    }

    //Dead Proxy model
    
    public class DeadProxy
    {
        public int id { get; set; }
        public string? ip { get; set; }
        public string? port { get; set; }
        public string? location { get; set; }
        public int? delay { get; set; }
        public string? protocol { get; set; }
        public string? defence { get; set; }
        public DateTime? lastCheck { get; set; }
        public DateTime? insertDateTime { get; set; }
        public int? rate { get; set; }
        public int? tested { get; set; }
        public int? testOK { get; set; }
        public int? lastRate { get; set; }
        public string? login { get; set; }
        public string? pass { get; set; }
        public bool? status { get; set; }
    }
    

    //IG Proxy Token model
    public class IG_Proxy_Token
    {
        public string proxy { get; set; } = null!;
        public string? xUserId { get; set; }
        public string? xUserToken { get; set; }
        public string? afUserId { get; set; }
        public DateTime? dt { get; set; }
    }

    //IG Proxy Token Archive model
    public class IG_Proxy_Token_Archive
    {
        public string proxy { get; set; } = null!;
        public string? xUserId { get; set; }
        public string? xUserToken { get; set; }
        public string? afUserId { get; set; }
        public DateTime? dt { get; set; }
    }

    public class PI_Parameter
    {
        public int id { get; set; }
        public string? note { get; set; }
        public int? value { get; set; }
        public string? valueS { get; set; }
    }
    //Proxy to Domain model
    public class Proxy_to_Domain
    {
        public string ip { get; set; } = "";
        public string port { get; set; } = "";
        public string protocol { get; set; } = "";
        public string domain { get; set; } = "";
        public int tested { get; set; } = 0;
        public int testOK { get; set; } = 0;
        public int rate { get; set; } = 0;
        public DateTime? lastCheck { get; set; } = DateTime.Now;
        public string? userAgent { get; set; } = "";
    }
    // Custom comparer for the Proxy_to_Domain class
    class ProxyToDomainIpPortProtocolComparer : IEqualityComparer<Proxy_to_Domain>
    {
        // Products are equal if their names and product numbers are equal.
        public bool Equals(Proxy_to_Domain? x, Proxy_to_Domain? y)
        {
            //Check whether the compared objects reference the same data.
            if (Object.ReferenceEquals(x, y)) return true;
            //Check whether any of the compared objects is null.
            if (x is null || y is null) return false;
            //Check whether the products' properties are equal.
            return x.protocol == y.protocol && x.ip == y.ip && x.port == y.port;
        }

        // If Equals() returns true for a pair of objects
        // then GetHashCode() must return the same value for these objects.

        public int GetHashCode(Proxy_to_Domain ptd)
        {
            //Check whether the object is null
            if (Object.ReferenceEquals(ptd, null)) return 0;

            //Get hash code for the protocol field if it is not null.
            int hashProductProtocol = ptd.protocol == null ? 0 : ptd.protocol.GetHashCode();

            //Get hash code for the ip field.
            int hashProductIp = ptd.ip == null ? 0 : ptd.ip.GetHashCode();

            //Get hash code for the port field.
            int hashProductPort = ptd.port == null ? 0 : ptd.port.GetHashCode();

            //Calculate the hash code for the product.
            return hashProductProtocol ^ hashProductIp ^ hashProductPort;
        }
    }
}
