namespace Admin.IdentityServer.Domain
{
    public interface IEntityVersion
    {
        /// <summary>
        /// 版本
        /// </summary>
        long Version { get; set; }
    }
}