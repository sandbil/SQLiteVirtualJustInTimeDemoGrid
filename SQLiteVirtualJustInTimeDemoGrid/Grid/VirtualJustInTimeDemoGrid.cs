using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Data.SQLite;

using System.Diagnostics;


namespace VirtualJustInTimeDemoGrid
{
    public partial class VirtualJustInTimeDemoGrid : UserControl
    {
        public VirtualJustInTimeDemoGrid()
        {
            InitializeComponent();
            if (DesignMode) return;
            dataGridView1.Rows.CollectionChanged += new System.ComponentModel.CollectionChangeEventHandler(this.DataGridViewRowCollection1_CollectionChanged);

        }

        public int rowPerPage = 15;

        private Cache memoryCache;
        private SQLiteDataStore sqliteDS;
        private string connectionString;
        private string table;
        private string[] _fields;
        public string filter;

        public event EventHandler CurrentChanged;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]

        public int RowCount
        {
            get => dataGridView1.RowCount;
            set => dataGridView1.RowCount = value;
        }

        public void AddColumns(string[] fields)
        {
            dataGridView1.Columns.Add("rowid", "rowid");
            foreach (string fl in fields)
                dataGridView1.Columns.Add(fl, fl);
        }

        public DataGridViewRowCollection Rows
        {
            get
            {
                return dataGridView1.Rows;
            }
        }
        public int? CurrentRowIndex
        {
            get
            {
                return dataGridView1.CurrentRow?.Index;
            }
        }
        public Cache MemCache
        {
            get => memoryCache;
            set => memoryCache = value;
        }

        public void Open(string connectionStr, string openTable, string[] fields, string filterStr = null)
        {
            Debug.WriteLine("(Open before) RowCount: " + RowCount + ", " + "DataCount: " + memoryCache?.AllRowCount + ", RowIndex: " + dataGridView1.CurrentRow?.Index);
            connectionString = connectionStr; table = openTable; _fields = fields; filter = filterStr;
            sqliteDS = new SQLiteDataStore(connectionString, table, _fields, filter);
            memoryCache = new Cache(sqliteDS, rowPerPage);
            MemCache = memoryCache;
            dataGridView1.Rows.Clear();
            dataGridView1.RowCount = sqliteDS.RowCount;
            dataGridView1.Refresh();
            Debug.WriteLine("(Open after) RowCount: " + RowCount + ", " + "DataCount: " + memoryCache.AllRowCount + ", RowIndex: " + dataGridView1.CurrentRow?.Index);

        }

        public void UpdateCurRow(string[,] updatePrms)
        {
            try
            {
                int rowid = Convert.ToInt32(dataGridView1.CurrentRow.Cells["rowid"].Value);
                int curInd = dataGridView1.CurrentRow.Index;
                sqliteDS.UpdateSQLiteRow(updatePrms, rowid);
                memoryCache.RefreshPage(curInd);
                Debug.WriteLine("(before update RowCount) RowCount: " + RowCount + ", " + "DataCount: " + memoryCache.AllRowCount + ", RowIndex: " + curInd);
                dataGridView1.RowCount = memoryCache.AllRowCount;
                dataGridView1.Refresh();
                Debug.WriteLine("(after update) RowCount: " + RowCount + ", " + "DataCount: " + memoryCache.AllRowCount + ", RowIndex: " + curInd);

            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.Message);
                Debug.WriteLine("(UpdateCurRow) " + ex.Message);
            }

        }
        private void dataGridView1_CellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= dataGridView1.RowCount) return;
            if (memoryCache?.AllRowCount == 0 || dataGridView1.RowCount != memoryCache?.AllRowCount)
                return;

            e.Value = memoryCache.RetrieveElement(e.RowIndex, e.ColumnIndex);
        }

        private bool IsCellValid(int columnIndex, int rowIndex)
        {
            if (columnIndex == -1) return false;
            if (rowIndex == -1) return false;
            if (rowIndex >= RowCount) return false;
            return true;
        }
        private void dataGridView1_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {

            if (!IsCellValid(e.ColumnIndex, e.RowIndex)) return;

            var r = dataGridView1.Rows[e.RowIndex];
            string status = r.Cells["status"].Value.ToString();
            r.Cells[e.ColumnIndex].Style.BackColor = status == "status-3" ? Color.LightPink :
                status == "status-2" ? Color.Yellow :
                status == "status-0" ? Color.LightGray : Color.White;
        }

        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            Debug.WriteLine("(SelectionChanged) RowCount: " + RowCount + ", " + "DataCount: " + memoryCache.AllRowCount + ", RowIndex: " + dataGridView1.CurrentRow?.Index);
            if (CurrentRowIndex != null)
                CurrentChanged(sender, e);
        }

        private void dataGridView1_UserDeletingRow(object sender, DataGridViewRowCancelEventArgs e)
        {
            try
            {
                if (false)//(MessageBox.Show("Вы действительно хотите удалить эту строку?", "Внимание", MessageBoxButtons.YesNo) == DialogResult.No)
                {
                    e.Cancel = true;
                }
                else
                {
                    int rowid = Convert.ToInt32(e.Row.Cells["rowid"].Value);
                    sqliteDS.DeleteSQLiteRow(rowid);
                    memoryCache.RefreshPage(e.Row.Index);
                    Debug.WriteLine("(UserDeletingRow) RowCount: " + RowCount + ", " + "DataCount: " + memoryCache.AllRowCount + ", RowIndex: " + e.Row.Index);
                }
            }
            catch (Exception ex)
            {
                Debug.Assert(false, ex.Message);
                e.Cancel = true;
            }
        }

        private void DataGridViewRowCollection1_CollectionChanged(Object sender, CollectionChangeEventArgs e)
        {
            Debug.WriteLine("(CollectionChanged) RowCount: " + RowCount + ", " + "DataCount: " + memoryCache.AllRowCount + ", RowIndex: " + e.Element);
        }

        private void dataGridView1_RowsRemoved(object sender, DataGridViewRowsRemovedEventArgs e)
        {
            Debug.WriteLine("(RowsRemoved) RowCount: " + RowCount + ", " + "DataCount: " + memoryCache.AllRowCount + ", RowIndex: " + e.RowIndex);
        }
    }
}
