using System;

namespace Admin.IdentityServer.Domain
{
    public interface IEntityUpdate<TKey> where TKey : struct
    {
        TKey? ModifiedUserId { get; set; }
        string ModifiedUserName { get; set; }
        DateTime? ModifiedTime { get; set; }
    }
}