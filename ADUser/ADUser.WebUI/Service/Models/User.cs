using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ADUser.WebUI.Service.Models
{
    public class User
    {
        public object Id { get; set; }

        public string OU { get; set; }

        public string Account { get; set; }

        public string Password { get; set; }

        public string Name { get; set; }

        public bool IsEnabled { get; set; }

        public DateTime LastLogonTime { get; set; }

        public DateTime CreateTime { get; set; }

        public static User Convert(System.DirectoryServices.SearchResult model)
        {
            var temp = new Models.User();

            temp.Id = model.Properties["objectguid"][0];

            var ou = model.GetDirectoryEntry().Parent.InvokeGet("OU");

            temp.OU = ou == null ? "system" : ou.ToString();
            temp.Name = model.Properties["name"][0].ToString();
            temp.Account = model.Properties["samaccountname"][0].ToString();
            temp.IsEnabled = model.Properties["useraccountcontrol"][0].ToString() != "546" ? true : false;
            temp.CreateTime = model.Properties["whencreated"][0].ToString().ToDateTime();
            temp.LastLogonTime = ConvertLongDateTime(model.Properties["lastlogon"][0].ToString().ToInt64());

            return temp;
        }

        private static DateTime ConvertLongDateTime(long d)
        {
            return TimeZone.CurrentTimeZone.ToLocalTime(System.Convert.ToDateTime("1601-1-1 00:00:00")).Add(new TimeSpan(d));
        }
    }
}