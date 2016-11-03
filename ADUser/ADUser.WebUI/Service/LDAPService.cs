using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Linq;
using System.Web;

namespace ADUser.WebUI.Service
{
    public class LDAPService
    {
        public IEnumerable<Models.User> QueryList(string ou, string key)
        {
            var des = Unit.Connection.GetDirectoryEntrys(ou);

            var list = new List<Models.User>();

            foreach (SearchResult v in des)
            {
                list.Add(Models.User.Convert(v));
            }

            return list;
        }

        public bool Save(Models.User model)
        {
            return Unit.Connection.CreateUser(model.OU, model.Name, model.Account, model.Password);
        }

        public bool SetPassword(string account, string password)
        {
            return Unit.Connection.SetPassword(account, password);
        }

        public bool SetEnable(string account)
        {
            return Unit.Connection.EnableUser(account);
        }

        public bool SetDisabled(string account)
        {
            return Unit.Connection.DisableUser(account);
        }

        public bool Delete(string account)
        {
            return Unit.Connection.DeleteUser(account);
        }
    }
}