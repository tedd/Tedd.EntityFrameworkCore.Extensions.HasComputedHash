namespace Tedd.EntityFrameworkCore.Extensions.HasComputedHash;

public enum HashMethod
{
    [Obsolete("Obsolete since SQL Server 2016 (13.x)")]
    MD2,
    [Obsolete("Obsolete since SQL Server 2016 (13.x)")]
    MD4,
    [Obsolete("Obsolete since SQL Server 2016 (13.x)")]
    MD5,
    [Obsolete("Obsolete since SQL Server 2016 (13.x)")]
    SHA,
    [Obsolete("Obsolete since SQL Server 2016 (13.x)")]
    SHA1, 
    SHA2_256, 
    SHA2_512
}