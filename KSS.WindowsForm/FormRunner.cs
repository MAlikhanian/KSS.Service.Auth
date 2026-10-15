using Form = System.Windows.Forms.Form;
using KSS.Helper;
using KSS.Helper.Enum;
using KSS.Helper.Model;
using KSS.Helper.Enum.Base;

namespace KSS.WindowsForm
{
    public partial class FormRunner : Form
    {
        public FormRunner()
        {
            InitializeComponent();
        }

        private async void bRunner02_Click(object sender, EventArgs e)
        {
            var client = new APIClient();

            var response = await client.Get<List<Model>>("/_000_ERP_Task/ToList/1700105");

            MessageBox.Show("Done - Count: " + response.Count);
        }

        private async void bRunner01_Click(object sender, EventArgs e)
        {
            var client = new APIClient();

            //var response = await client.Post<_000_ERP_Task, Filter>("_000_ERP_Task/Find/", new Filter { DataType = DataType.Long, Value = 1 });
            //var response = await client.Post<List<_000_ERP_Task>, Filter>("_000_ERP_Task/ToListByFilter/", new Filter { DataType = DataType.Byte, Value = 2 });

            //MessageBox.Show("Done - Count: " + response.Count);
        }
    }
}
