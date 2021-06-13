using FreeSql.DataAnnotations;
using System.ComponentModel;

namespace Admin.IdentityServer.Domain
{
    public interface IEntity
    {
    }

    public class Entity<TKey> : IEntity
    {
        /// <summary>
        /// 编号
        /// </summary>
        [Description("编号")]
        [Column(Position = 1)]
        public virtual TKey Id { get; set; }
    }

    public class Entity : Entity<long>
    {
    }
}