using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(Search_Engine.Startup))]
namespace Search_Engine
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
            
        }

    }
}
