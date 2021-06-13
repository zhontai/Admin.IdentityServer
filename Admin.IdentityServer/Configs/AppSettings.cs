using FreeSql;

namespace Admin.IdentityServer.Configs
{
    public class AppSettings
    {
        /// <summary>
        /// Api地址，默认 https://localhost:5000
        /// </summary>
        public string Urls { get; set; } = "https://localhost:5000";

        /// <summary>
        /// 跨域地址，默认 http://*:8000
        /// </summary>
        public string[] CorUrls { get; set; }// = new[]{ "http://*:8000" };

        /// <summary>
        /// 证书
        /// </summary>
        public Certificate Certificate { get; set; } = new Certificate();

        /// <summary>
        /// Swagger文档
        /// </summary>
        public bool Swagger { get; set; } = false;

        /// <summary>
        /// 健康检查
        /// </summary>
        public bool Health { get; set; } = false;

        /// <summary>
        /// 租户类型
        /// </summary>
        public bool Tenant { get; set; }

        /// <summary>
        /// 数据库
        /// </summary>
        public Db Db { get; set; } = new Db();

        /// <summary>
        /// Admin前端客户端
        /// </summary>
        public AdminUI AdminUI { get; set; } = new AdminUI();
    }

    /// <summary>
    /// 证书
    /// </summary>
    public class Certificate
    {
        /// <summary>
        /// 地址
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// 密码
        /// </summary>
        public string Password { get; set; }
    }

    /// <summary>
    /// 数据库
    /// </summary>
    public class Db
    {
        /// <summary>
        /// 监听Curd操作
        /// </summary>
        public bool Curd { get; set; } = false;

        /// <summary>
        /// 数据库类型
        /// </summary>
        public DataType Type { get; set; } = DataType.Sqlite;

        /// <summary>
        /// 数据库字符串
        /// </summary>
        public string ConnectionString { get; set; } = "Data Source=|DataDirectory|\\admindb.db; Pooling=true;Min Pool Size=1";
    }

    /// <summary>
    /// Admin前端客户端
    /// </summary>
    public class AdminUI
    {
        /// <summary>
        /// 登录成功跳转地址
        /// </summary>
        public string[] RedirectUris { get; set; }

        /// <summary>
        /// 退出登录跳转地址
        /// </summary>
        public string[] PostLogoutRedirectUris { get; set; }

        /// <summary>
        /// 密钥
        /// </summary>
        public string Secret { get; set; } = "secret";

        /// <summary>
        /// token有效时间
        /// </summary>
        public int AccessTokenLifetime { get; set; } = 7200;
    }
}