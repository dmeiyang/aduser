using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Web;

namespace ADUser.WebUI.Unit
{
    public class Connection
    {
        #region 链接信息
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
        #endregion

        /// <summary>
        /// 获得DirectoryEntry对象实例
        /// </summary>
        /// <returns>DirectoryEntry对象实例</returns>
        private static DirectoryEntry GetDirectoryObject()
        {
            return new DirectoryEntry(string.Format("{0}/{1}", ADPath, LDAPDomain), ADUser, ADPassword, AuthenticationTypes.Secure);
        }

        /// <summary>
        /// 获得DirectoryEntry对象实例，根据指定用户名和密码
        /// </summary>
        /// <returns>DirectoryEntry对象实例</returns>
        private static DirectoryEntry GetDirectoryObject(string userName, string password)
        {
            return new DirectoryEntry(string.Format("{0}/{1}", ADPath, LDAPDomain), userName, password, AuthenticationTypes.None);
        }

        /// <summary>
        /// 判断是否存在指定的ou组织结构
        /// </summary>
        /// <param name="ou">组织结构</param>
        /// <returns>true，存在；false，不存在</returns>
        public static bool IsExistOU(string ou)
        {
            try
            {
                var entry = GetDirectoryObject();

                var ouentry = entry.Children.Find("OU=" + ou);

                return ouentry != null;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 判断是否存在指定的account用户账号
        /// </summary>
        /// <param name="account">用户账号</param>
        /// <returns>true，存在；false，不存在</returns>
        public static bool IsExistAccount(string account)
        {
            var ds = new DirectorySearcher();

            ds.SearchRoot = GetDirectoryObject();

            ds.Filter = "(&(&(objectCategory=person)(objectClass=user))(samaccountname=" + account + "))";

            ds.SearchScope = SearchScope.Subtree;

            return ds.FindOne() != null;
        }

        /// <summary>
        /// 根据组织结构获取用户对象集合
        /// </summary>
        /// <param name="ou">组织结构</param>
        /// <returns>用户对象集合</returns>
        public static SearchResultCollection GetDirectoryEntrys(string ou = null)
        {
            if (!string.IsNullOrEmpty(ou) && !IsExistOU(ou))
            {
                return null;
            }

            var ds = new DirectorySearcher();

            if (string.IsNullOrEmpty(ou))
            {
                ds.SearchRoot = new DirectoryEntry(string.Format("{0}/{1}", ADPath, LDAPDomain), ADUser, ADPassword);
            }
            else
            {
                ds.SearchRoot = new DirectoryEntry(string.Format("{0}/OU={1},{2}", ADPath, ou, LDAPDomain), ADUser, ADPassword);
            }

            ds.Filter = "(&(objectCategory=person)(objectClass=user))";

            ds.SearchScope = SearchScope.Subtree;

            ds.Sort = new SortOption("Name", System.DirectoryServices.SortDirection.Ascending);

            return ds.FindAll();
        }

        /// <summary>
        /// 根据用户账号获取用户对象
        /// </summary>
        /// <param name="account">用户账号</param>
        /// <returns>用户对象</returns>
        public static DirectoryEntry GetDirectoryEntry(string account)
        {
            var ds = new DirectorySearcher();

            ds.SearchRoot = GetDirectoryObject();

            ds.Filter = "(&(&(objectCategory=person)(objectClass=user))(samaccountname=" + account + "))";

            ds.SearchScope = SearchScope.Subtree;

            return ds.FindOne().GetDirectoryEntry();
        }

        /// <summary>
        /// 创建用户
        /// </summary>
        /// <param name="ou">组织结构</param>
        /// <param name="name">用户名称</param>
        /// <param name="account">用户账号</param>
        /// <param name="password">用户密码</param>
        /// <returns>true，添加成功；false，添加失败</returns>
        public static bool CreateUser(string ou, string name, string account, string password)
        {
            if (IsExistAccount(account) || !IsExistOU(ou))
                return false;

            try
            {
                //1、创建新用户
                var entry = GetDirectoryObject();//new DirectoryEntry(ADPath, ADUser, ADPassword, AuthenticationTypes.Secure);

                var subEntry = ou == "Users" ? entry.Children.Find(string.Format("CN={0}", ou)) : entry.Children.Find(string.Format("OU={0}", ou));

                var deUser = subEntry.Children.Add(string.Format("CN={0}", name), "user");

                deUser.Properties["samaccountname"].Value = account;

                deUser.CommitChanges();

                //EnableUser(account);

                //SetPassword(account, password);

                //2、设置用户密码
                deUser.AuthenticationType = AuthenticationTypes.Secure;

                object[] pwdarray = new object[] { password };

                object ret = deUser.Invoke("SetPassword", password);

                deUser.CommitChanges();

                //3、启用新用户

                //UF_DONT_EXPIRE_PASSWD 0x10000  
                int exp = (int)deUser.Properties["useraccountcontrol"].Value;
                deUser.Properties["useraccountcontrol"].Value = exp | 0x0001;
                deUser.CommitChanges();

                //UF_ACCOUNTDISABLE 0x0002  
                int val = (int)deUser.Properties["useraccountcontrol"].Value;
                deUser.Properties["useraccountcontrol"].Value = val & ~0x0002;
                deUser.CommitChanges();

                deUser.Close();

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 启用用户
        /// </summary>
        /// <param name="account">用户账号</param>
        /// <returns>true，启用成功；false，启用失败</returns>
        public static bool EnableUser(string account)
        {
            try
            {
                var deUser = GetDirectoryEntry(account);

                //UF_DONT_EXPIRE_PASSWD（密码永不过期） 0x10000  
                int exp = (int)deUser.Properties["useraccountcontrol"].Value;
                deUser.Properties["useraccountcontrol"].Value = exp | 0x0001;
                deUser.CommitChanges();

                //UF_ACCOUNTDISABLE（用户账号禁用） 0x0002  
                int val = (int)deUser.Properties["useraccountcontrol"].Value;
                deUser.Properties["useraccountcontrol"].Value = val & ~0x0002;
                deUser.CommitChanges();

                deUser.Close();

                return true;
            }
            catch
            {
                return false;
            }
            
        }

        /// <summary>
        /// 禁用用户
        /// </summary>
        /// <param name="account">用户账号</param>
        /// <returns>true，启用成功；false，启用失败</returns>
        public static bool DisableUser(string account)
        {
            try
            {
                var deUser = GetDirectoryEntry(account);

                //UF_ACCOUNTDISABLE（用户账号禁用） 0x0002  
                deUser.Properties["useraccountcontrol"].Value = "546";
                deUser.CommitChanges();

                deUser.Close();

                return true;
            }
            catch
            {
                return false;
            }

        }

        /// <summary>
        /// 设置用户密码，管理员使用
        /// </summary>
        /// <param name="account">用户账号</param>
        /// <param name="password">用户密码</param>
        /// <returns>true，设置成功；false，设置失败</returns>
        public static bool SetPassword(string account, string password)
        {
            try
            {
                var deUser = GetDirectoryEntry(account);
                deUser.Invoke("SetPassword", new object[] { password });
                deUser.CommitChanges();

                deUser.Close();

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 删除用户
        /// </summary>
        /// <param name="account">用户账号</param>
        /// <returns>true，删除成功；false，删除失败</returns>
        public static bool DeleteUser(string account)
        {
            try
            {
                var deUser = GetDirectoryEntry(account);
                deUser.DeleteTree();
                deUser.CommitChanges();

                deUser.Close();

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}