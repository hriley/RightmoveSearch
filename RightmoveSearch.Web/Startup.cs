using Microsoft.Owin;
using Owin;
using RightmoveSearch.Web;

[assembly: OwinStartup(typeof(Startup))]
namespace RightmoveSearch.Web
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
