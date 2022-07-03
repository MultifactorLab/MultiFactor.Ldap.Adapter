using MultiFactor.Ldap.Adapter.Configuration;

namespace MultiFactor.Ldap.Adapter
{
    partial class AdapterService
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            this.ServiceName = ServiceConfiguration.ServiceUnitName;
        }
    }
}
