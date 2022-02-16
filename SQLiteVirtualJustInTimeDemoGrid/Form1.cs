using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using VirtualJustInTimeDemoGrid;

namespace SQLiteVirtualJustInTimeDemoGrid
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private string connectionString = "Data Source = ..\\..\\db\\test.db";
        private string table = "table1";
        private string filterStr = null;
        private string[] fields = { "status", "level", "testField", "fld"  };



        private void Form1_Load(object sender, EventArgs e)
        {
            DataRetriever retriever = new DataRetriever(connectionString, table, fields, filterStr);
            retriever.DropTable();
            retriever.CreateTable();
            retriever.LoadTestData();
            virtualJustInTimeDemoGrid1.AddColumns(fields);
            virtualJustInTimeDemoGrid1.Open(connectionString, table, fields, filterStr);

            cmBoxLevel.Items.Add("All levels");
            for (int i = 0; i <= 9; i++)
                cmBoxLevel.Items.Add(String.Format("level-{0}", i));
            cmBoxLevel.SelectedIndex = 0;

            cmBoxStatus.Items.Add("All statuses");
            for (int i = 0; i <= 4; i++)
                cmBoxStatus.Items.Add(String.Format("status-{0}", i));
            cmBoxStatus.SelectedIndex = 0;




        }



        private void tsbCreate_Click(object sender, EventArgs e)
        {

        }

        private void virtualJustInTimeDemoGrid1_CurrentChanged(object sender, EventArgs e)
        {
            int rowIndex = (int)virtualJustInTimeDemoGrid1.CurrentRowIndex;
            tBtestField.Text = virtualJustInTimeDemoGrid1.Rows[rowIndex].Cells[3].Value?.ToString();
            tBFld.Text = virtualJustInTimeDemoGrid1.Rows[rowIndex].Cells[4].Value?.ToString();
        }

        private void tsbDelete_Click(object sender, EventArgs e)
        {
            virtualJustInTimeDemoGrid1.Focus();
            SendKeys.Send("{DEL}");

        }

        private void cmBox_TextChanged(object sender, EventArgs e)
        {
            if (cmBoxStatus.SelectedIndex != 0)
                filterStr = String.Format(" where status = '{0}'", cmBoxStatus.SelectedItem.ToString());
            else
                filterStr = null;

            if (cmBoxLevel.SelectedIndex != 0)
            {
                if (filterStr != null)
                    filterStr += String.Format(" and level = '{0}'", cmBoxLevel.SelectedItem.ToString());
                else
                    filterStr = String.Format(" where level = '{0}'", cmBoxLevel.SelectedItem.ToString());
            }

            virtualJustInTimeDemoGrid1.Open(connectionString, table, fields, filterStr);
            virtualJustInTimeDemoGrid1.Focus();
            toolStripStatusLabel1.Text = String.Format("loaded {0} rec", virtualJustInTimeDemoGrid1.RowCount);

        }

        private void tsbUpdate_Click(object sender, EventArgs e)
        {
            int rowIndex = (int)virtualJustInTimeDemoGrid1.CurrentRowIndex;
            string[,] updateData = new string[3, 2] {  { "status", "status-0" }, { "level", "" }, { "fld", "" } };

            virtualJustInTimeDemoGrid1.UpdateCurRow(updateData);
        }
    }
}
