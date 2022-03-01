using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
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

        private static string dbPath = "db";
        private string connectionString = $"Data Source = {dbPath}\\test.db";
        private string table = "table1";
        private string filterStr = null;
        private string[] fields = { "status", "level", "testField", "fld"  };
        Boolean ignoreTextChanged = false;



        private void Form1_Load(object sender, EventArgs e)
        {
            if (!Directory.Exists(dbPath))
                Directory.CreateDirectory(dbPath);
            SQLiteDataStore sqliteDS = new SQLiteDataStore(connectionString, table, fields, filterStr);
            sqliteDS.DropTable();
            sqliteDS.CreateTable();
            sqliteDS.LoadTestData();
            vmGrid1.AddColumns(fields);
            vmGrid1.Open(connectionString, table, fields, filterStr);
            toolStripStatusLabel1.Text = $"total {vmGrid1.RowCount} rec";

            cmBoxLevel.Items.Add("All levels");
            for (int i = 0; i <= 9; i++)
            {
                cmBoxLevel.Items.Add(item: $"level-{i}");
            }
            cmBoxLevel.SelectedIndex = 0;

            cmBoxStatus.Items.Add("All statuses");
            for (int i = 0; i <= 8; i++)
            {
                cmBoxStatus.Items.Add(item: $"status-{i}");
            }

            cmBoxStatus.SelectedIndex = 0;
        }



        private void tsbCreate_Click(object sender, EventArgs e)
        {

        }

        private void virtualJustInTimeDemoGrid1_CurrentChanged(object sender, EventArgs e)
        {
            int rowIndex = (int)vmGrid1.CurrentRowIndex;
            ignoreTextChanged = true;
            tBtestField.Text = vmGrid1.Rows[rowIndex].Cells[3].Value?.ToString();
            tBFld.Text = vmGrid1.Rows[rowIndex].Cells[4].Value?.ToString();
            ignoreTextChanged = false;

        }

        private void tsbDelete_Click(object sender, EventArgs e)
        {
            vmGrid1.Focus();
            SendKeys.Send("{DEL}");
            toolStripStatusLabel1.Text = $"total {vmGrid1.RowCount} rec";
        }

        private void cmBox_SelectionChangeCommited(object sender, EventArgs e)
        {
            if (cmBoxStatus.SelectedIndex != 0)
                filterStr = $" where status = '{cmBoxStatus.SelectedItem.ToString()}'";
            else
                filterStr = null;

            if (cmBoxLevel.SelectedIndex != 0)
            {
                if (filterStr != null)
                    filterStr += $" and level = '{cmBoxLevel.SelectedItem.ToString()}'";
                else
                    filterStr = $" where level = '{cmBoxLevel.SelectedItem.ToString()}'";
            }

            vmGrid1.Open(connectionString, table, fields, filterStr);
            vmGrid1.Focus();
            toolStripStatusLabel1.Text = $"total {vmGrid1.RowCount} rec";

        }

        private void tsbUpdate_Click(object sender, EventArgs e)
        {
            List<KeyValuePair<string, object>> updateData = new List<KeyValuePair<string, object>>
            {
                new KeyValuePair<string, object>("status", "status-0"),
                new KeyValuePair<string, object>("level", ""),
                new KeyValuePair<string, object>("fld", "")
            };
            vmGrid1.UpdateCurRow(updateData);
            toolStripStatusLabel1.Text = $"total {vmGrid1.RowCount} rec";
        }

        private void tBtestField_TextChanged(object sender, EventArgs e)
        {
            if (ignoreTextChanged ) return;
            List<KeyValuePair<string, object>> updateData = new List<KeyValuePair<string, object>>
            {
                new KeyValuePair<string, object>("testField", tBtestField.Text)
            };
            vmGrid1.UpdateCurRow(updateData);
            toolStripStatusLabel1.Text = $"total {vmGrid1.RowCount} rec";
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (vmGrid1.RowCount <= 0) return;
            string[] fieldsForExport = { "txtAdr", "etalGuid" };
            DateTime date = DateTime.Now;
            string date_str = date.ToString("yyyy-MM-dd_HH-mm-ss");
            string strFilePath = $"{dbPath}\\test.db_{date_str}.csv";

            int exported = vmGrid1.ExportCurData(fieldsForExport, strFilePath);
            toolStripStatusLabel1.Text = $"exported {exported} rec";

        }
    }
}
