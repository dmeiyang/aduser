using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace LDAP.WebUI.Service
{
    public class LDAPHelper
    {
        /// <summary>
        /// 域名
        /// </summary>
        private static string Domain = "adtest.com";

        /// <summary>
        /// LDAP 地址
        /// </summary>
        private static string LDAPDomain = "DC=adtest,DC=com";

        /// <summary>
        /// LDAP绑定路径
        /// </summary>
        private static string ADPath = "LDAP://192.168.2.142";

        /// <summary>
        /// 登录帐号
        /// </summary>
        private static string ADUser = "Administrator";

        /// <summary>
        /// 登录密码
        /// </summary>
        private static string ADPassword = "@zy123456";

        private static IdentityImpersonation impersonate = new IdentityImpersonation(ADUser, ADPassword, Domain);

        /// <summary>
        /// 验证域账号合法性
        /// </summary>
        /// <param name="account">域账号</param>
        /// <param name="password">密码</param>
        /// <returns>true，合法账号；false，非法账号</returns>
        public static bool VerifyAccountLegitimacy(string account, string password)
        {
            try
            {
                var entry = new DirectoryEntry(ADPath, account, password);

                entry.RefreshCache();

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 获取用户列表
        /// </summary>
        /// <param name="group">分组名称，必填项</param>
        /// <returns>用户列表</returns>
        public static List<Models.SearchResult> QueryList(string group)
        {
            var ds = new DirectorySearcher();

            if (string.IsNullOrEmpty(group))
            {
                ds.SearchRoot = new DirectoryEntry(string.Format("{0}/{1}", ADPath, LDAPDomain), ADUser, ADPassword);
            }
            else
            {
                ds.SearchRoot = new DirectoryEntry(string.Format("{0}/OU={1},{2}", ADPath, group, LDAPDomain), ADUser, ADPassword);
            }


            ds.Filter = "(&(objectCategory=person)(objectClass=user))";

            ds.SearchScope = SearchScope.Subtree;

            ds.Sort = new SortOption("Name", System.DirectoryServices.SortDirection.Ascending);

            var list = new List<Models.SearchResult>();

            foreach (SearchResult v in ds.FindAll())
            {
                list.Add(ConvertEntryToSearchResult(v));
            }

            return list;
        }

        /// <summary>
        /// 获取用户
        /// </summary>
        /// <param name="name">用户姓名</param>
        /// <returns>对象实体</returns>
        public static Models.SearchResult QueryDetailByName(string name)
        {
            var ds = new DirectorySearcher();

            ds.SearchRoot = new DirectoryEntry(string.Format("{0}/{1}", ADPath, LDAPDomain), ADUser, ADPassword);

            ds.Filter = "(&(&(objectCategory=person)(objectClass=user))(cn=" + name + "))";

            ds.SearchScope = SearchScope.Subtree;

            var entry = ds.FindOne();

            return ConvertEntryToSearchResult(entry);
        }

        /// <summary>
        /// 获取用户
        /// </summary>
        /// <param name="account">用户账号</param>
        /// <returns>用户实体</returns>
        public static Models.SearchResult QueryDetailByAccount(string account)
        {
            var ds = new DirectorySearcher();

            ds.SearchRoot = new DirectoryEntry(string.Format("{0}/{1}", ADPath, LDAPDomain), ADUser, ADPassword);

            ds.Filter = "(&(&(objectCategory=person)(objectClass=user))(samaccountname=" + account + "))";

            ds.SearchScope = SearchScope.Subtree;

            var entry = ds.FindOne();

            return ConvertEntryToSearchResult(entry);
        }

        /// <summary>
        /// 判断用户账号是否已存在
        /// </summary>
        /// <param name="account">用户账号</param>
        /// <returns>true，存在；false，不存在</returns>
        public static bool IsAccountExist(string account)
        {
            var ds = new DirectorySearcher();

            ds.SearchRoot = new DirectoryEntry(string.Format("{0}/{1}", ADPath, LDAPDomain), ADUser, ADPassword);

            ds.Filter = "(&(&(objectCategory=person)(objectClass=user))(samaccountname=" + account + "))";

            ds.SearchScope = SearchScope.Subtree;

            var entry = ds.FindOne();

            return entry != null;
        }

        /// <summary>
        /// 添加用户
        /// </summary>
        /// <param name="model">用户实体</param>
        /// <returns>true，操作成功；false，操作失败</returns>
        public static bool InsertUser(Models.User model)
        {
            if (IsAccountExist(model.Account))
                return false;

            try
            {
                var entry = new DirectoryEntry(ADPath, ADUser, ADPassword, AuthenticationTypes.Secure);

                var subEntry = model.Group== "Users"? entry.Children.Find(string.Format("CN={0}", model.Group)): entry.Children.Find(string.Format("OU={0}", model.Group));

                var deUser = subEntry.Children.Add(string.Format("CN={0}", model.Name), "user");

                deUser.Properties["samaccountname"].Value = model.Account;

                deUser.CommitChanges();

                deUser.Close();

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static Models.SearchResult ConvertEntryToSearchResult(SearchResult model)
        {
            var temp = new Models.SearchResult();

            temp.Path = model.Path;

            temp.Name = model.Properties["name"][0].ToString();
            temp.Account = model.Properties["samaccountname"][0].ToString();
            temp.LogonCount = model.Properties["logoncount"][0].ToString().ToInt32();
            temp.CreateTime = model.Properties["whencreated"][0].ToString().ToDateTime();
            temp.LastLogonTime = ConvertLongDateTime(model.Properties["lastlogon"][0].ToString().ToInt64());
            temp.PwdLastSetTime = ConvertLongDateTime(model.Properties["pwdlastset"][0].ToString().ToInt64());

            return temp;
        }

        private static DateTime ConvertLongDateTime(long d)
        {
            return TimeZone.CurrentTimeZone.ToLocalTime(Convert.ToDateTime("1601-1-1 00:00:00")).Add(new TimeSpan(d));
        }
    }

    /// <summary>
    /// 用户模拟角色类。实现在程序段内进行用户角色模拟。
    /// </summary>
    public class IdentityImpersonation
    {
        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool LogonUser(String lpszUsername, String lpszDomain, String lpszPassword, int dwLogonType, int dwLogonProvider, ref IntPtr phToken);

        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public extern static bool DuplicateToken(IntPtr ExistingTokenHandle, int SECURITY_IMPERSONATION_LEVEL, ref IntPtr DuplicateTokenHandle);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public extern static bool CloseHandle(IntPtr handle);

        // 要模拟的用户的用户名、密码、域(机器名)
        private String _sImperUsername;
        private String _sImperPassword;
        private String _sImperDomain;
        // 记录模拟上下文
        private WindowsImpersonationContext _imperContext;
        private IntPtr _adminToken;
        private IntPtr _dupeToken;
        // 是否已停止模拟
        private Boolean _bClosed;


        /// <summary>
        ///构造函数
        ///所要模拟的用户的用户名
        ///所要模拟的用户的密码
        ///所要模拟的用户所在的域
        /// </summary>
        /// <param name="impersonationUsername"></param>
        /// <param name="impersonationPassword"></param>
        /// <param name="impersonationDomain"></param>
        public IdentityImpersonation(String impersonationUsername, String impersonationPassword, String impersonationDomain)
        {
            _sImperUsername = impersonationUsername;
            _sImperPassword = impersonationPassword;
            _sImperDomain = impersonationDomain;

            _adminToken = IntPtr.Zero;
            _dupeToken = IntPtr.Zero;
            _bClosed = true;
        }

        /// <summary>
        /// 析构函数
        /// </summary>
        ~IdentityImpersonation()
        {
            if (!_bClosed)
            {
                StopImpersonate();
            }
        }

        /// <summary>
        /// 开始身份角色模拟。
        /// </summary>
        /// <returns></returns>
        public Boolean BeginImpersonate()
        {
            Boolean bLogined = LogonUser(_sImperUsername, _sImperDomain, _sImperPassword, 2, 0, ref _adminToken);

            if (!bLogined)
            {
                return false;
            }

            Boolean bDuped = DuplicateToken(_adminToken, 2, ref _dupeToken);

            if (!bDuped)
            {
                return false;
            }

            WindowsIdentity fakeId = new WindowsIdentity(_dupeToken);
            _imperContext = fakeId.Impersonate();

            _bClosed = false;

            return true;
        }

        /// <summary>
        /// 停止身分角色模拟。
        /// </summary>
        public void StopImpersonate()
        {
            _imperContext.Undo();
            CloseHandle(_dupeToken);
            CloseHandle(_adminToken);
            _bClosed = true;
        }
    }
}
