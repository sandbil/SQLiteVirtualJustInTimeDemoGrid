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
            // to avoid errors specified row index is out of range.
            dataGridView1.Rows.CollectionChanged += new System.ComponentModel.CollectionChangeEventHandler(this.DataGridViewRowCollection1_CollectionChanged);
        }

        public int rowPerPage = 60;

        private Cache memoryCache;
        private SQLiteDataStore sqliteDS;
        private string connectionString;
        private string table;
        public string filter;

        private bool ignore = false;

        public event EventHandler CurrentChanged;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]

        public int RowCount => dataGridView1.RowCount;
        public DataGridViewRowCollection Rows => dataGridView1.Rows;
        public int? CurrentRowIndex => dataGridView1.CurrentRow?.Index;
        public void AddColumns(string[] fields)
        {
            dataGridView1.Columns.Add("rowid", "rowid");
            foreach (string fl in fields)
                dataGridView1.Columns.Add(fl, fl);
        }
        public void Open(string connectionStr, string openTable, string[] fields, string filterStr = null)
        {
            try
            {
                ignore = true;
                connectionString = connectionStr; table = openTable;  filter = filterStr;
                sqliteDS = new SQLiteDataStore(connectionString, table, fields, filter);
                memoryCache = new Cache(sqliteDS, rowPerPage);
                dataGridView1.Rows.Clear();
                dataGridView1.RowCount = sqliteDS.RowCount;
                dataGridView1.Refresh();
                ignore = false;
                Debug.WriteLine($"(Open after) RowCount: {RowCount}, DataCount: {memoryCache?.AllRowCount}, RowIndex: {dataGridView1.CurrentRow?.Index}");
            }
            catch (Exception ex)
            {
                ignore = false;
                Debug.WriteLine($"Error (Open) {ex.Message}");
            }

        }
        public void UpdateCurRow(List<KeyValuePair<string, object>> updateData)
        {
            try
            {
                if (CurrentRowIndex == null || ignore) return;
                
                int rowid = Convert.ToInt32(dataGridView1.CurrentRow.Cells["rowid"].Value);
                int curInd = (int) CurrentRowIndex;
                sqliteDS.UpdateSQLiteRow(updateData, rowid);
                memoryCache.RefreshPages(curInd);
                Debug.WriteLine($"(before update RowCount) RowCount: {RowCount}, DataCount: {memoryCache?.AllRowCount}, RowIndex: {curInd}");

                dataGridView1.RowCount = memoryCache.AllRowCount;
                dataGridView1.Refresh();
                Debug.WriteLine($"(after update RowCount) RowCount: {RowCount}, DataCount: {memoryCache?.AllRowCount}, RowIndex: {curInd}");
                if (CurrentRowIndex != null)
                    CurrentChanged(dataGridView1, EventArgs.Empty); //! Force emit a CurrentChanged event
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error (UpdateCurRow) {ex.Message}");
            }

        }
        public int ExportCurData(string[] fieldsForExport, string strFilePath, string encoding = "UTF-8", bool haveHeaders = false, int rowForExport = 10000)
        {
            try
            {
                if (CurrentRowIndex == null || ignore) return 0;
                
                int expRowCount = sqliteDS.ExportToCSV(fieldsForExport, strFilePath, encoding, haveHeaders, rowForExport);
                return expRowCount;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error (ExportCurData) {ex.Message}");
                return -1;
            }
        }

        private void dataGridView1_CellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
        {
            if (!IsCellValid(e.ColumnIndex, e.RowIndex) || memoryCache?.AllRowCount == 0) return;
            e.Value = memoryCache.RetrieveElement(e.RowIndex, dataGridView1.Columns[e.ColumnIndex].Name);
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
            int pn = memoryCache.PageNumRowCached((int)dataGridView1.CurrentRow?.Index);
            Debug.WriteLine($"(SelectionChanged) RowCount: {RowCount}, DataCount: {memoryCache?.AllRowCount}, RowIndex: {dataGridView1.CurrentRow?.Index}, Page: {pn}");
            if (CurrentRowIndex != null)
                CurrentChanged(sender, e);
        }

        private void dataGridView1_UserDeletingRow(object sender, DataGridViewRowCancelEventArgs e)
        {
            try
            {
                if (CurrentRowIndex == null || ignore) return;

                if (false)//(MessageBox.Show("Вы действительно хотите удалить эту строку?", "Внимание", MessageBoxButtons.YesNo) == DialogResult.No)
                {
                    e.Cancel = true;
                }
                else
                {
                    int rowid = Convert.ToInt32(e.Row.Cells["rowid"].Value);
                    sqliteDS.DeleteSQLiteRow(rowid);
                    memoryCache.RefreshPages(e.Row.Index);
                    Debug.WriteLine($"(UserDeletingRow) RowCount: {RowCount}, DataCount: {memoryCache?.AllRowCount}, RowIndex: {e.Row.Index}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine( ex.Message);
                e.Cancel = true;
            }
        }

        // for diagnostic purposes and 
        // to avoid errors specified row index is out of range.

        private void DataGridViewRowCollection1_CollectionChanged(Object sender, CollectionChangeEventArgs e)
        {
            Debug.WriteLine($"(CollectionChanged) RowCount: {RowCount}, DataCount: {memoryCache?.AllRowCount}, RowIndex: {e.Element}");
        }

        // for diagnostic purpose only
        private void dataGridView1_RowsRemoved(object sender, DataGridViewRowsRemovedEventArgs e)
        {
            Debug.WriteLine($"(RowsRemoved) RowCount: {RowCount}, DataCount: {memoryCache?.AllRowCount}, RowIndex: {e.RowIndex}");
        }
    }
}
