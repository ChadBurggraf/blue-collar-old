
namespace WebInProc35
{
    using System;
    using System.Web.UI;

    public partial class Default : Page
    {
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            new ExampleJob().Enqueue();
        }
    }
}
