using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.DirectoryServices;

namespace ADUser.WebUI.Controllers
{
    public class UserController : BasicController
    {
        protected Service.LDAPService LDAPService { get { return new Service.LDAPService(); } }

        public void Test()
        {
            foreach (var v in Unit.Connection.GetDirectoryEntrys())
            {
                Response.Write(v.ToJsonByJsonNet());

                Response.Write("<hr/>");
            }
        }

        public ActionResult Index(string ou, string key)
        {
            var list = LDAPService.QueryList(ou, key);

            return View(list);
        }

        public ActionResult Add()
        {
            ViewBag.Title = "新建域用户";

            return PartialView(new Service.Models.User() { OU = "IT" });
        }

        public ActionResult Save(Service.Models.User model)
        {
            if (!ModelState.IsValid)
                return JRFaild();

            return JRCommonHandleResult(LDAPService.Save(model));
        }

        public ActionResult SetPassword(string account)
        {
            ViewBag.Title = "设置密码";

            return View((object)account);
        }

        [HttpPost]
        public ActionResult SetPassword(string account, string password)
        {
            return JRCommonHandleResult(LDAPService.SetPassword(account, password));
        }

        public ActionResult SetEnable(string account)
        {
            return JRCommonHandleResult(LDAPService.SetEnable(account));
        }

        public ActionResult SetDisabled(string account)
        {
            return JRCommonHandleResult(LDAPService.SetDisabled(account));
        }

        public ActionResult Delete(string accounts)
        {
            var array = accounts.ToSplit(',');

            foreach (var v in array)
            {
                LDAPService.Delete(v);
            }

            return JRCommonHandleResult(true);
        }
    }
}