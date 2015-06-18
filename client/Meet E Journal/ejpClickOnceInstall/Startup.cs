using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(ejpClickOnceInstall.Startup))]
namespace ejpClickOnceInstall
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
