namespace Admin.IdentityServer.Domain
{
    public interface IEntitySoftDelete
    {
        /// <summary>
        /// 是否删除
        /// </summary>
        bool IsDeleted { get; set; }
    }
}