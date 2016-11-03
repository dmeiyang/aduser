using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(ADUser.WebUI.Startup))]
namespace ADUser.WebUI
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
