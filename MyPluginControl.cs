using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using XrmToolBox.Extensibility;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk;
using McTools.Xrm.Connection;
using Excel = Microsoft.Office.Interop.Excel;
using System.IO;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Messages;
using System.Xml;
using System.Collections;
using System.ServiceModel;
using System.Runtime.InteropServices;

namespace DataImport
{
    public partial class MyPluginControl : PluginControlBase
    {
        // CREATE EXCEL OBJECTS.
        Excel.Application xlApp = new Excel.Application();
        Excel.Workbook xlWorkBook;
        Excel.Worksheet xlWorkSheet;
        Excel.Range xlRange;
        EntityMetadata resultsaved;
        EntityMetadata lkpresultsaved;
        RichTextBox richTextBoxErrors = new RichTextBox();
        RichTextBox richTextBoxImported = new RichTextBox();
        RichTextBox richTextBoxAll = new RichTextBox();
        RichTextBox richTextBoxWarning = new RichTextBox();
        //DataGridViewComboBoxCell dcc; //??
        string sFileName;
        string strentityname;
        bool strIsKey;
        bool IsReadyToImport = false;
        string qestr;
        int iRow, iCol = 1;
        bool flaglookup;
        int lookupscount;
        string mtextView = "";
        StringBuilder boxall = new StringBuilder();
        StringBuilder boxwarning = new StringBuilder();
        StringBuilder boxerror = new StringBuilder();
        StringBuilder boxsuccess = new StringBuilder();
        //BackgroundWorker _worker;
        private Settings mySettings;

        public MyPluginControl()
        {
            InitializeComponent();
        }
        public void MyPluginControl_Load(object sender, System.EventArgs e)
        {
            comboBox1.SelectedIndex = 0;
            crmAction.SelectedIndex = 0;
            textView.SelectedIndex = 0;
            optionSetVL.SelectedIndex = 0;
            ExecuteMethod(InitEntities);
        }
        
        public void InitEntities()
        {
            WorkAsync(new WorkAsyncInfo
            {
                Message = "Getting entities",
                Work = (worker, args) =>
                {
                    RetrieveAllEntitiesResponse metaDataResponse = new RetrieveAllEntitiesResponse();
                    RetrieveAllEntitiesRequest retrieveAllEntitiesRequest = new RetrieveAllEntitiesRequest
                    {
                        RetrieveAsIfPublished = true,
                        EntityFilters = EntityFilters.Attributes
                    };

                    retrieveAllEntitiesRequest.EntityFilters = EntityFilters.Entity;
                    // Execute the request.
                    args.Result = (RetrieveAllEntitiesResponse)Service.Execute(retrieveAllEntitiesRequest);


                },
                PostWorkCallBack = (args) =>
                {
                    if (args.Error != null)
                    {
                        MessageBox.Show(args.Error.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    pickedEntity.Items.Clear();
                    lkpTargetEntity.Items.Clear();
                    var result = args.Result as RetrieveAllEntitiesResponse;
                    if (result != null)
                    {
                        var entities = result.EntityMetadata;
                        foreach (EntityMetadata Entity in entities)
                        {
                            pickedEntity.Items.Add(Entity.LogicalName);
                            lkpTargetEntity.Items.Add(Entity.LogicalName);
                        }
                    }
                }
            });
        }
        private void StartBackgroundWork(int i)
        {
            double perr = (i-1)/(1.0 * (xlRange.Rows.Count - 1)) * 100;

            //labelprogress.Text = "Import Progress "+perr.ToString("F") + "%";
        }

        private void InitEntityFields()
        {
            if (pickedEntity.SelectedItem == null)
            {
                //MessageBox.Show("Please load entities first and pick your entity then press this button.");
                //ExecuteMethod(InitEntities);
                return;
            }
            CRMField.Items.Clear();

            strentityname = pickedEntity.SelectedItem.ToString();
            WorkAsync(new WorkAsyncInfo
            {
                Message = "Getting entity fields",
                Work = (worker, args) =>
                {


                    Dictionary<string, string> attributesData = new Dictionary<string, string>();
                    RetrieveEntityRequest retrieveEntityRequest = new RetrieveEntityRequest
                    {
                        EntityFilters = EntityFilters.All,
                        LogicalName = strentityname
                    };

                    // Execute the request
                    args.Result = (RetrieveEntityResponse)Service.Execute(retrieveEntityRequest);
                },
                PostWorkCallBack = (args) =>
                {
                    if (args.Error != null)
                    {
                        MessageBox.Show(args.Error.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    var result = args.Result as RetrieveEntityResponse;
                    resultsaved = result.EntityMetadata;
                    if (result != null)
                    {
                        foreach (object attribute in resultsaved.Attributes)
                        {
                            AttributeMetadata a = (AttributeMetadata)attribute;
                            if (a.AttributeType.ToString() == "DateTime" || a.AttributeType.ToString() == "String" || a.AttributeType.ToString() == "Picklist" || a.AttributeType.ToString() == "Boolean" || a.AttributeType.ToString().ToLower() == "integer" || a.AttributeType.ToString().ToLower() == "money" || a.AttributeType.ToString() == "Lookup" || a.AttributeType.ToString() == "Customer")
                                CRMField.Items.Add(a.LogicalName.ToString());
                        }
                    }
                }
            });
        }

        private void InitLookupFields(string myentity, int thatRow)
        {
            if (myentity == null || myentity == "")
            {
                return;
            }
            //lkpTargetfield.Items.Clear();

            WorkAsync(new WorkAsyncInfo
            {
                Message = "Getting entity fields",
                Work = (worker, args) =>
                {
                    Dictionary<string, string> attributesData = new Dictionary<string, string>();
                    RetrieveEntityRequest retrieveEntityRequest = new RetrieveEntityRequest
                    {
                        EntityFilters = EntityFilters.All,
                        LogicalName = myentity
                    };

                    // Execute the request
                    args.Result = (RetrieveEntityResponse)Service.Execute(retrieveEntityRequest);
                },
                PostWorkCallBack = (args) =>
                {
                    if (args.Error != null)
                    {
                        MessageBox.Show(args.Error.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    var result = args.Result as RetrieveEntityResponse;
                    lkpresultsaved = result.EntityMetadata;
                    if (result != null)
                    {
                        DataGridViewComboBoxCell stateCell = (DataGridViewComboBoxCell)(dataGridView1.Rows[thatRow].Cells[5]);
                        foreach (object attribute in lkpresultsaved.Attributes)
                        {
                            AttributeMetadata a = (AttributeMetadata)attribute;
                            if (/*a.AttributeType.ToString() == "DateTime" ||*/ a.AttributeType.ToString() == "String" /*|| a.AttributeType.ToString() == "Picklist" || a.AttributeType.ToString() == "Boolean" || a.AttributeType.ToString().ToLower() == "integer" || a.AttributeType.ToString().ToLower() == "money"*/)
                            {
                                stateCell.Items.Add(a.LogicalName.ToString());
                            }
                        }
                    }
                }
            });
        }


        /* private void MyPluginControl_Load(object sender, EventArgs e)
         {
             //ShowInfoNotification("This is a notification that can lead to XrmToolBox repository", new Uri("https://github.com/MscrmTools/XrmToolBox"));
             // Loads or creates the settings for the plugin
             if (!SettingsManager.Instance.TryLoad(GetType(), out mySettings))
             {
                 mySettings = new Settings();

                 LogWarning("Settings not found => a new settings file has been created!");
             }
             else
             {
                 LogInfo("Settings found and loaded");
             }
         }*/


        private void TsbClose_Click(object sender, EventArgs e)
        {
            CloseTool();
        }


        private void TsbSample_Click(object sender, EventArgs e)
        {
            ExecuteMethod(InitEntities);
        }

        private void MyPluginControl_OnCloseTool(object sender, EventArgs e)
        {
            // Before leaving, save the settings
            SettingsManager.Instance.Save(GetType(), mySettings);
        }

        /// <summary>
        /// This event occurs when the connection has been updated in XrmToolBox
        /// </summary>
        public override void UpdateConnection(IOrganizationService newService, ConnectionDetail detail, string actionName, object parameter)
        {
            base.UpdateConnection(newService, detail, actionName, parameter);
            if (mySettings != null && detail != null)
            {
                mySettings.LastUsedOrganizationWebappUrl = detail.WebApplicationUrl;
                LogInfo("Connection has changed to: {0}", detail.WebApplicationUrl);
            }
        }

        private void Button1_Click(object sender, EventArgs e)
        {



        }

        private void OpenFileDialog1_FileOk(object sender, CancelEventArgs e)
        {

        }

        private void DataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

      /*  private void DataGridView1_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            //GET OUR COMBO OBJECT
            var combo = e.Control as ComboBox;
            if (combo != null)
            {
                // AVOID ATTACHMENT TO MULTIPLE EVENT HANDLERS
                combo.SelectedIndexChanged -= new EventHandler(Combo_SelectedIndexChanged);

                //THEN NOW ADD
                combo.SelectedIndexChanged += Combo_SelectedIndexChanged;
            }
        }

        private void Combo_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selected = (sender as ComboBox).SelectedItem.ToString();
        }
        */

        private void Button1_Click_1(object sender, EventArgs e)
        {

        }

        // GET DATA FROM EXCEL AND POPULATE COMB0 BOX.
        private void ReadExcel(string sFile)
        {

            WorkAsync(new WorkAsyncInfo
            {
                Message = "Reading Excel File..",
                Work = (worker, args) =>
                {
                    xlApp = new Excel.Application();
                    xlWorkBook = xlApp.Workbooks.Open(sFile);    // WORKBOOK TO OPEN THE EXCEL FILE.
                    xlWorkSheet = xlWorkBook.Worksheets[1];      // NAME OF THE SHEET.
                    xlRange = xlWorkSheet.UsedRange;
                },
                PostWorkCallBack = (args) =>
                {
                    for (iCol = 1; iCol <= xlRange.Columns.Count; iCol++)  // START FROM THE SECOND ROW.
                    {
                        if (xlRange.Cells[1, iCol].value == null)
                        {
                            break;      // BREAK LOOP.
                        }
                        else
                        {
                            dataGridView1.Rows.Add(xlRange.Cells[1, iCol].value);
                        }
                    }
                    xlWorkBook.Close();
                    xlApp.Quit();
                }
            });
        }

        private void SplitContainer2_Panel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void ListView1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void PickedEntity_DropDownClosed(object sender, EventArgs e)
        {//SelectedIndexChanged
            if (pickedEntity.SelectedItem != null)
                ExecuteMethod(InitEntityFields);
        }
        private void SetTextBox1()
        {
            if (textView.SelectedItem.ToString() == "📙 ALL")
            {
                richTextBox1.Text = richTextBoxAll.Text;
            }
            else if (textView.SelectedItem.ToString() == "✓ SUCCESS")
            {
                richTextBox1.Text = richTextBoxImported.Text;
            }
            else if (textView.SelectedItem.ToString() == "❌ ERRORS")
            {
                richTextBox1.Text = richTextBoxErrors.Text;
            }
            else if (textView.SelectedItem.ToString() == "⚠ WARNINGS")
            {
                richTextBox1.Text = richTextBoxWarning.Text;
            }
        }
        private void TextView_DropDownClosed(object sender, EventArgs e)
        {
            //SetTextBox1();
            
            if (textView.SelectedItem.ToString() == "📙 ALL")
            {
                richTextBox1.Text = richTextBoxAll.Text;
            }
            else if (textView.SelectedItem.ToString() == "✓ SUCCESS")
            {
                richTextBox1.Text = richTextBoxImported.Text;
            }
            else if (textView.SelectedItem.ToString() == "❌ ERRORS")
            {
                richTextBox1.Text = richTextBoxErrors.Text;
            }
            else if (textView.SelectedItem.ToString() == "⚠ WARNINGS")
            {
                richTextBox1.Text = richTextBoxWarning.Text;
            }

        }
        private void EmptyDataGrid()
        {
            dataGridView1.Rows.Clear();
            dataGridView1.Columns["lkpTargetEntity"].Visible = false;
            dataGridView1.Columns["lkpTargetfield"].Visible = false;
            dataGridView1.Columns["Truevalue"].Visible = false;
            dataGridView1.Columns["Falsevalue"].Visible = false;
        }

        private void GetFile()
        {
            openFileDialog1.Title = "Excel File to Import";
            openFileDialog1.FileName = "";
            openFileDialog1.Filter = "Excel File|*.xlsx;*.xls";
            DialogResult result = openFileDialog1.ShowDialog(); // Show the dialog.
            if (result == DialogResult.OK) // Test result.
            {
                EmptyDataGrid();
                string file = openFileDialog1.FileName;
                try
                {
                    sFileName = openFileDialog1.FileName;

                    if (sFileName.Trim() != "")
                    {
                        ReadExcel(sFileName);
                    }
                }
                catch (IOException)
                {

                }
            }
        }
        private void ToolStripButton1_Click(object sender, EventArgs e)
        {
            GetFile();
        }

        private void Label1_Click(object sender, EventArgs e)
        {

        }

        private void ToolStripButton1_Click_1(object sender, EventArgs e)
        {
            if (dataGridView1.RowCount == 0)
            {
                MessageBox.Show("Please BROWSE EXCEL FILE first and Pick your entity and fields mapping before Lauching the Import to CRM.");
                return;
            }
            dataGridView1.CurrentCell = dataGridView1.Rows[0].Cells[0];
            
            if (IsReadyToImport)
            {
                ImportExcel();
            }
            else
            {
                MessageBox.Show("WARNING: Action will not be launched. Please press the button 'PROCESS FIELDS' before importing to CRM.");
            }
        }

        private void ToolStripButton2_Click(object sender, EventArgs e)
        {
            ExecuteMethod(InitEntityFields);
        }

        private void ToolStripButton3_Click(object sender, EventArgs e)
        {
            ///CLEAR ALL
            xlWorkBook = null;
            xlWorkSheet = null;
            xlRange = null;
            xlApp = null;
            pickedEntity.SelectedItem = null;
            EmptyDataGrid();
            label2.Visible = false;
            optionSetVL.Visible = false;
            optionSetVL.SelectedItem = null;
            label4.Visible = false;
            comboBox1.Visible = false;
            comboBox1.SelectedItem = null;
            crmAction.SelectedIndex = 0;
            CRMField.Items.Clear();
            dataGridView1.Refresh();
            richTextBox1.Text = "";
            richTextBoxErrors.Text = "";
            richTextBoxImported.Text = "";
            richTextBoxAll.Text = "";
            richTextBoxWarning.Text = "";
            flaglookup = false;
            lookupscount = 0;
            IsReadyToImport = false;
        }

        
        private void ToolStripButton2_Click_1(object sender, EventArgs e)
        {
            if(dataGridView1.RowCount == 0)
            {
                MessageBox.Show("Please BROWSE EXCEL FILE and Pick your entity and fields mapping first.");
                return;
            }
            dataGridView1.CurrentCell = dataGridView1.Rows[0].Cells[0];
            string acrmfield;
            int dRow;
            lookupscount = 0;
            for (dRow = 0; dRow < dataGridView1.RowCount; dRow++)
            {
                string lkpentityname = Convert.ToString((dataGridView1.Rows[dRow].Cells[4] as DataGridViewComboBoxCell).FormattedValue.ToString());
                acrmfield = Convert.ToString((dataGridView1.Rows[dRow].Cells[2] as DataGridViewComboBoxCell).FormattedValue.ToString());
                foreach (object attribute in resultsaved.Attributes)
                {
                    AttributeMetadata a = (AttributeMetadata)attribute;
                    if (a.LogicalName.ToString() == acrmfield)  //Find the CRM field between the metadata
                    {
                        DataGridViewCheckBoxCell chk = dataGridView1.Rows[dRow].Cells[3] as DataGridViewCheckBoxCell;
                        if (a.AttributeType.ToString() == "Lookup" || a.AttributeType.ToString() == "Customer") // check if the CRM field is of type Lookup
                        {
                            dataGridView1.Columns["lkpTargetEntity"].Visible = true;
                            dataGridView1.Columns["lkpTargetfield"].Visible = true;
                            label4.Visible = true;
                            comboBox1.Visible = true;
                            //Flag row as lookup
                            lookupscount++;
                            chk.Value = true;
                            DataGridViewComboBoxCell data1 = dataGridView1.Rows[dRow].Cells[4] as DataGridViewComboBoxCell;
                            data1.ReadOnly = false;
                            data1.DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton;
                            DataGridViewComboBoxCell data2 = dataGridView1.Rows[dRow].Cells[5] as DataGridViewComboBoxCell;
                            data2.ReadOnly = false;
                            data2.DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton;
                            InitLookupFields(lkpentityname, dRow);
                            
                        }
                        else
                        {
                            //Décocher la case
                            chk.Value = false;
                            //Bloquer la grille des non lookup
                            DataGridViewComboBoxCell data1 = dataGridView1.Rows[dRow].Cells[4] as DataGridViewComboBoxCell;
                            data1.ReadOnly = true;
                            data1.Value = null;
                            data1.DisplayStyle = DataGridViewComboBoxDisplayStyle.Nothing;
                            DataGridViewComboBoxCell data2 = dataGridView1.Rows[dRow].Cells[5] as DataGridViewComboBoxCell;
                            data2.ReadOnly = true;
                            data2.DisplayStyle = DataGridViewComboBoxDisplayStyle.Nothing;
                            data2.Value = null;
                        }
                        if(a.AttributeType.ToString() == "Boolean")
                        {
                            dataGridView1.Columns["Truevalue"].Visible = true;
                            dataGridView1.Columns["Falsevalue"].Visible = true;
                            DataGridViewCell databooltrue = dataGridView1.Rows[dRow].Cells[6] as DataGridViewCell;
                            databooltrue.ReadOnly = false;
                            databooltrue.Style.BackColor = Color.LightGray;
                            DataGridViewCell databoolfalse = dataGridView1.Rows[dRow].Cells[7] as DataGridViewCell;
                            databoolfalse.ReadOnly = false;
                            databoolfalse.Style.BackColor = Color.LightGray;

                            //fetch for true and false boolean values
                            RetrieveAttributeRequest retrieveAttributeRequest = new RetrieveAttributeRequest
                            {
                                EntityLogicalName = pickedEntity.SelectedItem.ToString(),
                                LogicalName = acrmfield,
                                RetrieveAsIfPublished = true
                            };
                            RetrieveAttributeResponse retrieveAttributeResponse = (RetrieveAttributeResponse)Service.Execute(retrieveAttributeRequest);
                            BooleanAttributeMetadata retrievedBooleanAttributeMetadata = (BooleanAttributeMetadata)retrieveAttributeResponse.AttributeMetadata;
                            string boolTextTrue = retrievedBooleanAttributeMetadata.OptionSet.TrueOption.Label.UserLocalizedLabel.Label;
                            string boolTextFalse = retrievedBooleanAttributeMetadata.OptionSet.FalseOption.Label.UserLocalizedLabel.Label;
                            dataGridView1.Rows[dRow].Cells["Truevalue"].Value = boolTextTrue;
                            dataGridView1.Rows[dRow].Cells["Falsevalue"].Value = boolTextFalse;
                        }
                        if(a.AttributeType.ToString() == "Picklist")
                        {
                            label2.Visible = true;
                            optionSetVL.Visible = true;
                            optionSetVL.SelectedIndex = 1;
                        }
                    }
                }
            }
            IsReadyToImport = true;
        }
        private void SplitContainer2_SplitterMoved(object sender, SplitterEventArgs e)
        {

        }

        private void DataGridView2_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void Lookuprelatedfields_Click(object sender, EventArgs e)
        {

        }

        private void PickedEntity_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void ProgressBar1_Click(object sender, EventArgs e)
        {

        }

        private void TextView_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void CopyText_Click(object sender, EventArgs e)
        {
            StringBuilder sb = new StringBuilder();
            foreach (string line in richTextBox1.Lines)
                sb.AppendLine(line);
            if (sb.Length != 0)
                Clipboard.SetText(sb.ToString());
            else
                MessageBox.Show("Logs are empty");
        }

        private void Labelprogress_Click(object sender, EventArgs e)
        {

        }

        private void Button1_Click_2(object sender, EventArgs e)
        {
            SetTextBox1();
        }

        private void ImportExcel()
        {
            //Verification que L'action CRM est bien choisie
            if (crmAction.SelectedItem == null)
            {
                //MessageBox.Show("Please choose a CRM action before Importing the file to CRM");
                return;
            }
            DataTable dt = new DataTable();

            foreach (DataGridViewColumn col in dataGridView1.Columns)
            {
                dt.Columns.Add(col.Name);
            }

            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                DataRow dRow = dt.NewRow();
                foreach (DataGridViewCell cell in row.Cells)
                {
                    dRow[cell.ColumnIndex] = cell.Value;
                    if (dRow[cell.ColumnIndex] == DBNull.Value)
                    {
                        if (cell.ColumnIndex == 1 || cell.ColumnIndex == 3)
                            dRow[cell.ColumnIndex] = false;
                        else
                        dRow[cell.ColumnIndex] = "";
                    }
                }
                dt.Rows.Add(dRow);
            }
            
            string mcrmAction = crmAction.SelectedItem.ToString();
            string mcomboBox1 = comboBox1.SelectedItem.ToString();
            string mtextView = textView.SelectedItem.ToString();
            string moptionSetVL = optionSetVL.SelectedItem.ToString();
            WorkAsync(new WorkAsyncInfo
            {
                Message = "Importing...",
                Work = (w, e) =>
                {

                    xlApp = new Excel.Application();
                    xlWorkBook = xlApp.Workbooks.Open(sFileName);   // WORKBOOK TO OPEN THE EXCEL FILE.
                    xlWorkSheet = xlWorkBook.Worksheets[1];  // NAME OF THE SHEET.
                    xlRange = xlWorkSheet.UsedRange;
                    string[] logicalnm = new string[xlRange.Columns.Count];
                    Guid _accountId = new Guid();
                    bool istoimport;

                    //richTextBox1.SelectionStart = richTextBox1.Text.Length;
                    //richTextBox1.SelectionLength = 0;
                    //richTextBox1.ScrollToCaret();
                    richTextBoxErrors.Text += "STARTING " + mcrmAction + " ACTION ON " + DateTime.Now.ToString() + Environment.NewLine;
                    richTextBoxImported.Text += "STARTING " + mcrmAction + " ACTION ON " + DateTime.Now.ToString() + Environment.NewLine;
                    richTextBoxAll.Text += "STARTING " + mcrmAction + " ACTION ON " + DateTime.Now.ToString();
                    richTextBoxWarning.Text += "STARTING " + mcrmAction + " ACTION ON " + DateTime.Now.ToString() + Environment.NewLine;
                    //SetTextBox1();
                    for (iRow = 2; iRow <= xlRange.Rows.Count; iRow++)  // START FROM THE SECOND ROW.
                    {
                        Entity record = null;
                        record = new Entity(strentityname);
                        double perr = (iRow - 1) / (1.0 * (xlRange.Rows.Count - 1)) * 100;
                        int perrr = Convert.ToInt32(perr);
                        w.ReportProgress(perrr);
                        istoimport = true;
                        flaglookup = false;

                        QueryExpression qe = new QueryExpression();
                        qe.EntityName = strentityname;
                        qe.ColumnSet = new ColumnSet();
                        for (iCol = 1; iCol <= xlRange.Columns.Count; iCol++)
                        {
                            if (xlRange[1, iCol].value == null)
                            {
                                break;
                            }
                            string myfieldlabel = dt.Rows[iCol - 1][2].ToString();  //GET FIELD NAME
                            logicalnm[iCol - 1] = myfieldlabel;
                            string myfieldtype = "";
                            if (xlRange.Cells[iRow, iCol].value == null)
                            {
                                foreach (object attribute in resultsaved.Attributes)
                                {
                                    AttributeMetadata a = (AttributeMetadata)attribute;
                                    if (a.LogicalName.ToString() == myfieldlabel)
                                    {
                                        myfieldtype = a.AttributeType.ToString();
                                        break;
                                    }
                                }
                                if (myfieldtype == "String")
                                {
                                    record[logicalnm[iCol - 1]] = "";
                                }
                                else if (myfieldtype == "Picklist" || myfieldtype == "Boolean" || myfieldtype == "DateTime" || myfieldtype == "Customer" || myfieldtype == "Lookup")
                                {
                                    record[logicalnm[iCol - 1]] = null;
                                }
                                if (dt.Rows[iCol - 1][1] == null)
                                    strIsKey = false;
                                else
                                    strIsKey = Convert.ToBoolean(dt.Rows[iCol - 1][1]);

                                if (strIsKey)
                                {
                                    //strIsKey = false;
                                    //istoimport = false;
                                    qe.Criteria.AddCondition(new ConditionExpression(logicalnm[iCol - 1], ConditionOperator.Null));
                                    richTextBoxWarning.Text += Environment.NewLine + "⚠LINE" + iRow + " - EXCEL LINE contains an empty key field: " + myfieldlabel;
                                    richTextBoxAll.Text += Environment.NewLine + "⚠LINE" + iRow + " - EXCEL LINE contains an empty key field: " + myfieldlabel;
                                    //SetTextBox1();
                                }
                            }
                            else //Record not empty
                            {
                                //SET UP FIELDS OF THE ENTITY
                                foreach (object attribute in resultsaved.Attributes)
                                {
                                    AttributeMetadata a = (AttributeMetadata)attribute;
                                    if (a.LogicalName.ToString() == myfieldlabel)
                                    {
                                        myfieldtype = a.AttributeType.ToString();
                                        if (myfieldtype == "Picklist")
                                        {
                                            //// OPTIONSET LABELS
                                            if (moptionSetVL == "OPTIONSET LABELS")
                                            {

                                                var picklistMetadata = (PicklistAttributeMetadata)resultsaved.Attributes.FirstOrDefault(myattribute => String.Equals(myattribute.LogicalName, a.LogicalName, StringComparison.OrdinalIgnoreCase));
                                                var options = (from o in picklistMetadata.OptionSet.Options
                                                               select new { Value = o.Value, Text = o.Label.UserLocalizedLabel.Label }).ToList();
                                                try
                                                {
                                                    int activeValue = (int)options.Where(o => o.Text == xlRange.Cells[iRow, iCol].value).Select(o => o.Value).FirstOrDefault();
                                                    record[logicalnm[iCol - 1]] = new OptionSetValue(activeValue);
                                                }
                                                catch (InvalidOperationException ex)
                                                {
                                                    richTextBoxAll.Text += Environment.NewLine + "⚠LINE" + iRow + " - Couldnt match Optionset Label : " + xlRange.Cells[iRow, iCol].value + " - " + ex.Message.ToString();
                                                    richTextBoxWarning.Text += Environment.NewLine + "⚠LINE" + iRow + " - Couldnt match Optionset Label : " + xlRange.Cells[iRow, iCol].value + " - " + ex.Message.ToString();
                                                    //SetTextBox1();
                                                }



                                            }
                                            else //OPTIONSET VALUES
                                            {
                                                if (xlRange.Cells[iRow, iCol].value.Equals(typeof(String)))
                                                {
                                                    int intvaluecell = 0;
                                                    try
                                                    {
                                                        intvaluecell = System.Convert.ToInt32(xlRange.Cells[iRow, iCol].value);
                                                        record[logicalnm[iCol - 1]] = new OptionSetValue(intvaluecell);
                                                    }
                                                    catch (FormatException)
                                                    {
                                                        MessageBox.Show("NOT A VALID INTEGER FOR AN OPTIONSETVALUE FIELD TYPE");
                                                    }
                                                    record[logicalnm[iCol - 1]] = new OptionSetValue();
                                                }
                                                else
                                                {
                                                    int avalue = (int)xlRange.Cells[iRow, iCol].value;
                                                    record[logicalnm[iCol - 1]] = new OptionSetValue(avalue);
                                                }
                                            }

                                            ////END OPTIONSET
                                        }

                                        /// if BOOLEAN
                                        else if (myfieldtype == "Boolean")
                                        {
                                            if (xlRange.Cells[iRow, iCol].value.ToString().ToLower() == (dt.Rows[iCol - 1]["Truevalue"].ToString().ToLower()))
                                            {
                                                record[logicalnm[iCol - 1]] = true;
                                            }
                                            else if (xlRange.Cells[iRow, iCol].value.ToString().ToLower() == (dt.Rows[iCol - 1]["Falsevalue"].ToString().ToLower()))
                                            {
                                                record[logicalnm[iCol - 1]] = false;
                                            }
                                            else
                                            {
                                                richTextBoxAll.Text += Environment.NewLine + "⚠LINE" + iRow + " - Couldnt match boolean value : " + xlRange.Cells[iRow, iCol].value + " - REASON: Only available options are: " + dt.Rows[iCol - 1]["Truevalue"].ToString() + " and " + dt.Rows[iCol - 1]["Falsevalue"].ToString();
                                                richTextBoxWarning.Text += Environment.NewLine + "⚠LINE" + iRow + " - Couldnt match boolean value : " + xlRange.Cells[iRow, iCol].value + " - REASON: Only available options are: " + dt.Rows[iCol - 1]["Truevalue"].ToString() + " and " + dt.Rows[iCol - 1]["Falsevalue"].ToString();
                                                //SetTextBox1();
                                            }
                                        }
                                        break;
                                    }
                                }
                                if (myfieldtype == "String")
                                    record[logicalnm[iCol - 1]] = xlRange.Cells[iRow, iCol].value;
                                else if (myfieldtype == "DateTime")
                                {
                                    if (xlRange.Cells[iRow, iCol].Equals(typeof(DateTime)))
                                    {
                                        record[logicalnm[iCol - 1]] = xlRange.Cells[iRow, iCol].value.ToDateTime();
                                    }
                                    else
                                    {
                                        try
                                        {
                                            record[logicalnm[iCol - 1]] = Convert.ToDateTime(xlRange.Cells[iRow, iCol].value);
                                        }
                                        catch (FormatException)
                                        {
                                            richTextBoxAll.Text += Environment.NewLine + "⚠LINE" + iRow + " - DateTime field : " + myfieldlabel + ": " + xlRange.Cells[iRow, iCol].value.ToString() + " is not valid.";
                                            richTextBoxWarning.Text += Environment.NewLine + "⚠LINE" + iRow + " - DateTime field : " + myfieldlabel + ": " + xlRange.Cells[iRow, iCol].value.ToString() + " is not valid.";
                                            //SetTextBox1();
                                        }
                                    }
                                }
                                else if (myfieldtype == "Money")
                                {

                                    if (xlRange.Cells[iRow, iCol].value.Equals(typeof(string)))
                                    {
                                        int currencyval = 0;
                                        try
                                        {
                                            currencyval = System.Convert.ToDecimal(xlRange.Cells[iRow, iCol].value);
                                            record[logicalnm[iCol - 1]] = new Money(currencyval);
                                        }
                                        catch (FormatException)
                                        {
                                            MessageBox.Show("NOT A VALID DECIMAL FOR A CURRENCY FIELD TYPE");
                                        }
                                    }
                                    else
                                    {
                                        decimal d = (decimal)xlRange.Cells[iRow, iCol].value / 100M;
                                        record[logicalnm[iCol - 1]] = new Money(d * 100);
                                    }
                                }
                                else if (myfieldtype == "Lookup" || myfieldtype == "Customer")
                                {
                                    flaglookup = true;
                                }

                                //Check if IS KEY
                                strIsKey = Convert.ToBoolean((dt.Rows[iCol - 1][1]).ToString());
                                Money mymoney;
                                OptionSetValue myoptionset;
                                Boolean boolvalentity;
                                if (strIsKey)
                                {
                                    if (myfieldtype == "Money")
                                    {
                                        mymoney = (Money)record[logicalnm[iCol - 1]];
                                        qestr = mymoney.Value.ToString();
                                        qe.Criteria.AddCondition(new ConditionExpression(logicalnm[iCol - 1], ConditionOperator.Equal, qestr));
                                    }
                                    else if (myfieldtype == "Picklist")
                                    {
                                        myoptionset = (OptionSetValue)record[logicalnm[iCol - 1]];
                                        qestr = myoptionset.Value.ToString();
                                        qe.Criteria.AddCondition(new ConditionExpression(logicalnm[iCol - 1], ConditionOperator.Equal, qestr));
                                    }
                                    else if (myfieldtype == "DateTime")
                                    {
                                        qe.Criteria.AddCondition(new ConditionExpression(logicalnm[iCol - 1], ConditionOperator.Equal, record[logicalnm[iCol - 1]]));
                                    }
                                    else if (myfieldtype == "Boolean")
                                    {
                                        try
                                        {
                                            boolvalentity = Convert.ToBoolean((record[logicalnm[iCol - 1]].ToString()));
                                            qe.Criteria.AddCondition(new ConditionExpression(logicalnm[iCol - 1], ConditionOperator.Equal, boolvalentity));
                                        }
                                        catch
                                        {
                                            MessageBox.Show("⚠PROCESS WILL ABORT. </br> EXCEL LINE" + iRow + " - Couldnt match boolean value : " + xlRange.Cells[iRow, iCol].value + " - REASON: Only available options are: " + dt.Rows[iCol - 1]["Truevalue"].ToString() + " and " + dt.Rows[iCol - 1]["Falsevalue"].ToString());
                                            return;
                                        }

                                    }
                                    else if (myfieldtype == "Lookup" || myfieldtype == "Customer")
                                    {

                                    }
                                    else // String
                                    {
                                        qestr = record[logicalnm[iCol - 1]].ToString();
                                        qe.Criteria.AddCondition(new ConditionExpression(logicalnm[iCol - 1], ConditionOperator.Equal, qestr));
                                    }

                                    ///ADD CONDITION FOR KEY
                                    if (myfieldtype != "Lookup" && myfieldtype != "Customer" && myfieldtype != "Boolean")
                                    {
                                        //dcc = (DataGridViewComboBoxCell)dataGridView1.Rows[iCol - 1].Cells[2];
                                        //int indexx = dcc.Items.IndexOf(dcc.Value);
                                        qe.Criteria.AddCondition(new ConditionExpression(logicalnm[iCol - 1], ConditionOperator.Equal, qestr));
                                    }
                                }
                            }
                        }
                        //START/////////////////////////////////////////////////////////////////////////////////////////////
                        if (flaglookup && mcrmAction != "DELETE")
                        {
                            QueryExpression lookupquery = new QueryExpression();
                            lookupquery.ColumnSet = new ColumnSet();
                            string lookuplglname;
                            string lkpnamefield;
                            string[] vec = new string[lookupscount];
                            int veccnt = 0;
                            bool boollkp;
                            for (int q = 0; q < dt.Rows.Count; q++) //All Rows of data grid, search for IS LOOKUPS
                            {
                                boollkp = Convert.ToBoolean((dt.Rows[q][3])); // IS LOOKUP?
                                if (boollkp == true) // IS Lookup = YES
                                {
                                    lkpnamefield = Convert.ToString((dt.Rows[q][2]).ToString());

                                    vec[veccnt] = lkpnamefield;
                                    veccnt++;
                                }
                            }
                            string[] distcVec = vec.Distinct().ToArray(); //Contains only unique names of lookup fields
                            bool[] distcKeyVec = new bool[distcVec.Length];
                            for (int m = 0; m < distcVec.Length; m++) // foreach unique lookupname
                            {
                                lookupquery.Criteria.Conditions.Clear();
                                for (int n = 0; n < dt.Rows.Count; n++) // Go search for all the lines in the table containing that lookup field
                                {

                                    if (distcVec[m] == Convert.ToString((dt.Rows[n][2]).ToString())) // When we find that the name of the lookup is the same as the distinct lookup value
                                    {
                                        distcKeyVec[m] = Convert.ToBoolean((dt.Rows[n][1])); // When we find that the name of the lookup is the same as the distinct lookup value
                                        lookuplglname = Convert.ToString((dt.Rows[n][4]).ToString());

                                        lookupquery.EntityName = lookuplglname;
                                        lookupquery.Criteria.AddCondition(Convert.ToString((dt.Rows[n][5]).ToString()), ConditionOperator.Equal, xlRange.Cells[iRow, n + 1].value);
                                        // Use iRow as Row. Call function to add a criteria -> lookupquery.Criteria.AddCondition();
                                    }
                                }
                                //FETCH FOR THE RECORD
                                EntityCollection mycollect = Service.RetrieveMultiple(lookupquery);
                                if (mycollect.Entities.Count > 0)
                                {
                                    if (mycollect.Entities.Count > 1)
                                    {
                                        if (mcomboBox1 == "IMPORT RECORD WITH CLEARED LOOKUP")
                                        {
                                            record[distcVec[m]] = null;
                                            richTextBoxWarning.Text += Environment.NewLine + "⚠LINE" + iRow + " - BLANK LOOKUP: " + distcVec[m].ToString() + " - REASON: Found " + mycollect.Entities.Count.ToString() + " records to insert in lookup.";
                                            richTextBoxAll.Text += Environment.NewLine + "⚠LINE" + iRow + " - BLANK LOOKUP: " + distcVec[m].ToString() + " - REASON: Found " + mycollect.Entities.Count.ToString() + " records to insert in lookup.";
                                        }
                                        else if (mcomboBox1 == "MAP THE FIRST FOUND RECORD TO THE LOOKUP")
                                        {
                                            record[distcVec[m]] = new EntityReference(mycollect[0].LogicalName, mycollect[0].Id);
                                            richTextBoxWarning.Text += Environment.NewLine + "⚠LINE" + iRow + " - LOOKUP ID: " + distcVec[m].ToString() + " = " + mycollect[0].Id.ToString() + " - REASON: Found " + mycollect.Entities.Count.ToString() + " records to insert in lookup and mapped the first one.";
                                            richTextBoxAll.Text += Environment.NewLine + "⚠LINE" + iRow + " - LOOKUP ID: " + distcVec[m].ToString() + " = " + mycollect[0].Id.ToString() + " - REASON: Found " + mycollect.Entities.Count.ToString() + " records to insert in lookup and mapped the first one.";
                                            if (distcKeyVec[m])
                                                qe.Criteria.AddCondition(distcVec[m], ConditionOperator.Equal, mycollect[0].Id);
                                        }
                                        else if (mcomboBox1 == "SKIP RECORD WITHOUT IMPORTING IT AT ALL")
                                        {
                                            istoimport = false;
                                            richTextBoxWarning.Text += Environment.NewLine + "⚠LINE" + iRow + " - LINE WILL NOT BE IMPORTED because of LOOKUP: " + distcVec[m].ToString() + " - REASON: Found " + mycollect.Entities.Count.ToString() + " records to insert in lookup.";
                                            richTextBoxAll.Text += Environment.NewLine + "⚠LINE" + iRow + " - LINE WILL NOT BE IMPORTED because of LOOKUP: " + distcVec[m].ToString() + " - REASON: Found " + mycollect.Entities.Count.ToString() + " records to insert in lookup.";
                                        }
                                    }
                                    else // Count==1 found entity
                                    {
                                        record[distcVec[m]] = new EntityReference(mycollect[0].LogicalName, mycollect[0].Id);
                                        if (distcKeyVec[m])
                                            qe.Criteria.AddCondition(distcVec[m], ConditionOperator.Equal, mycollect[0].Id);
                                    }
                                }
                                else // Didn't find a match
                                {
                                    record[distcVec[m]] = null;
                                    if (mcomboBox1 == "IMPORT RECORD WITH CLEARED LOOKUP")
                                    {
                                        richTextBoxWarning.Text += Environment.NewLine + "⚠LINE" + iRow + " - BLANK LOOKUP: " + distcVec[m].ToString() + " - REASON: Didn't find any record to insert in lookup.";
                                        richTextBoxAll.Text += Environment.NewLine + "⚠LINE" + iRow + " - BLANK LOOKUP: " + distcVec[m].ToString() + " - REASON: Didn't find any record to insert in lookup.";
                                    }
                                    else if (mcomboBox1 == "MAP THE FIRST FOUND RECORD TO THE LOOKUP")
                                    {
                                        richTextBoxWarning.Text += Environment.NewLine + "⚠LINE" + iRow + " - CLEARED LOOKUP: " + distcVec[m].ToString() + " - REASON: Didn't find any record to insert in lookup.";
                                        richTextBoxAll.Text += Environment.NewLine + "⚠LINE" + iRow + " - CLEARED LOOKUP: " + distcVec[m].ToString() + " - REASON: Didn't find any record to insert in lookup.";
                                    }
                                    else if (mcomboBox1 == "SKIP RECORD WITHOUT IMPORTING IT AT ALL")
                                    {
                                        istoimport = false;
                                        richTextBoxWarning.Text += Environment.NewLine + "⚠LINE" + iRow + " - LINE WILL NOT BE IMPORTED because of LOOKUP: " + distcVec[m].ToString() + " - REASON: Didn't find any record to insert in lookup.";
                                        richTextBoxAll.Text += Environment.NewLine + "⚠LINE" + iRow + " - LINE WILL NOT BE IMPORTED because of LOOKUP: " + distcVec[m].ToString() + " - REASON: Didn't find any record to insert in lookup.";
                                    }
                                }
                                //SetTextBox1();
                            }
                        }
                        ////END/////////////////////////////////////////////////////////////     

                        if (istoimport)
                        {

                            //CREATE
                            if (mcrmAction == "CREATE")
                            {
                                try
                                {
                                    _accountId = Service.Create(record);
                                    richTextBoxImported.Text += Environment.NewLine + "✓LINE" + iRow + " - CREATED: " + _accountId.ToString();
                                    richTextBoxAll.Text += Environment.NewLine + "✓LINE" + iRow + " - CREATED: " + _accountId.ToString();
                                }
                                catch (FaultException<OrganizationServiceFault> ex)
                                {
                                    richTextBoxErrors.Text += Environment.NewLine + "❌LINE" + iRow + " - Exception Message for CREATE: " + (ex.Message);
                                    richTextBoxAll.Text += Environment.NewLine + "❌LINE" + iRow + " - Exception Message for CREATE: " + (ex.Message);
                                }
                            }

                            //UPDATE
                            else if (mcrmAction == "UPDATE")
                            {
                                EntityCollection ec = Service.RetrieveMultiple(qe);
                                if (ec.Entities.Count > 0)
                                {
                                    foreach (Entity entity in ec.Entities)
                                    {
                                        record.Id = entity.Id;
                                        try
                                        {
                                            Service.Update(record);
                                            richTextBoxImported.Text += Environment.NewLine + "✓LINE" + iRow + " - UPDATED: " + entity.Id.ToString();
                                            richTextBoxAll.Text += Environment.NewLine + "✓LINE" + iRow + " - UPDATED: " + entity.Id.ToString();
                                        }
                                        catch (FaultException<OrganizationServiceFault> ex)
                                        {
                                            richTextBoxErrors.Text += Environment.NewLine + "❌LINE" + iRow + " - Exception Message for UPDATE: " + (ex.Message);
                                            richTextBoxAll.Text += Environment.NewLine + "❌LINE" + iRow + " - Exception Message for UPDATE: " + (ex.Message);
                                        }
                                    }
                                }
                                else
                                {
                                    richTextBoxErrors.Text += Environment.NewLine + "❌LINE" + iRow + " - NOT FOUND TO UPDATE";
                                    richTextBoxAll.Text += Environment.NewLine + "❌LINE" + iRow + " - NOT FOUND TO UPDATE";
                                }
                            }

                            //UPSERT
                            else if (mcrmAction == "UPSERT")
                            {
                                EntityCollection ec = Service.RetrieveMultiple(qe);
                                if (ec.Entities.Count > 0)
                                {
                                    foreach (Entity entity in ec.Entities)
                                    {
                                        record.Id = entity.Id;
                                        try
                                        {
                                            Service.Update(record);
                                            richTextBoxImported.Text += Environment.NewLine + "✓LINE" + iRow + " - UPDATED: " + entity.Id.ToString();
                                            richTextBoxAll.Text += Environment.NewLine + "✓LINE" + iRow + " - UPDATED: " + entity.Id.ToString();
                                        }
                                        catch (FaultException<OrganizationServiceFault> ex)
                                        {
                                            richTextBoxErrors.Text += Environment.NewLine + "❌LINE" + iRow + " - Exception Message for UPDATE: " + (ex.Message);
                                            richTextBoxAll.Text += Environment.NewLine + "❌LINE" + iRow + " - Exception Message for UPDATE: " + (ex.Message);
                                        }
                                    }
                                }
                                else
                                {
                                    try
                                    {
                                        _accountId = Service.Create(record);
                                        richTextBoxImported.Text += Environment.NewLine + "✓LINE" + iRow + " - CREATED: " + _accountId.ToString();
                                        richTextBoxAll.Text += Environment.NewLine + "✓LINE" + iRow + " - CREATED: " + _accountId.ToString();
                                    }
                                    catch (FaultException<OrganizationServiceFault> ex)
                                    {
                                        richTextBoxErrors.Text += Environment.NewLine + "❌LINE" + iRow + " - Exception Message for CREATE: " + (ex.Message);
                                        richTextBoxAll.Text += Environment.NewLine + "❌LINE" + iRow + " - Exception Message for CREATE: " + (ex.Message);
                                    }

                                }
                            }
                            else if (mcrmAction == "DELETE")
                            {
                                EntityCollection ec = Service.RetrieveMultiple(qe);
                                if (ec.Entities.Count > 0)
                                {
                                    foreach (Entity entity in ec.Entities)
                                    {
                                        record.Id = entity.Id;
                                        try
                                        {
                                            Service.Delete(strentityname, record.Id);
                                        }
                                        catch (FaultException<OrganizationServiceFault> ex)
                                        {
                                            richTextBoxErrors.Text += Environment.NewLine + "❌LINE" + iRow + " - Exception Message for DELETE: " + (ex.Message);
                                            richTextBoxAll.Text += Environment.NewLine + "❌LINE" + iRow + " - Exception Message for DELETE: " + (ex.Message);
                                        }
                                        richTextBoxImported.Text += Environment.NewLine + "✓LINE" + iRow + " - DELETED: " + entity.Id.ToString();
                                        richTextBoxAll.Text += Environment.NewLine + "✓LINE" + iRow + " - DELETED: " + entity.Id.ToString();
                                    }
                                }
                                else
                                {
                                    richTextBoxErrors.Text += Environment.NewLine + "❌LINE" + iRow + " - NOT FOUND TO DELETE: LINE" + iRow;
                                    richTextBoxAll.Text += Environment.NewLine + "❌LINE" + iRow + " - NOT FOUND TO DELETE: LINE" + iRow;
                                }
                            }
                            //SetTextBox1();
                            //richTextBox1.SelectionStart = richTextBox1.Text.Length;
                            //richTextBox1.SelectionLength = 0;
                            //richTextBox1.ScrollToCaret();

                        }
                    }
                    xlWorkBook.Close();
                    xlApp.Quit();
                    if (xlRange != null) Marshal.ReleaseComObject(xlRange);
                    if (xlWorkSheet != null) Marshal.ReleaseComObject(xlWorkSheet);
                    if (xlWorkBook != null) Marshal.ReleaseComObject(xlWorkBook);
                    if (xlApp != null) Marshal.ReleaseComObject(xlApp);

                    richTextBoxImported.Text += Environment.NewLine + Environment.NewLine + mcrmAction + " PROCESS FINISHED ON " + DateTime.Now.ToString() + Environment.NewLine + "-----------------------------------------------------------------------------------------------" + Environment.NewLine + Environment.NewLine;
                    richTextBoxErrors.Text += Environment.NewLine + Environment.NewLine + mcrmAction + " PROCESS FINISHED ON " + DateTime.Now.ToString() + Environment.NewLine + "-----------------------------------------------------------------------------------------------" + Environment.NewLine + Environment.NewLine;
                    richTextBoxWarning.Text += Environment.NewLine + Environment.NewLine + mcrmAction + " PROCESS FINISHED ON " + DateTime.Now.ToString() + Environment.NewLine + "-----------------------------------------------------------------------------------------------" + Environment.NewLine + Environment.NewLine;
                    richTextBoxAll.Text += Environment.NewLine + Environment.NewLine + mcrmAction + " PROCESS FINISHED ON " + DateTime.Now.ToString() + Environment.NewLine + "-----------------------------------------------------------------------------------------------" + Environment.NewLine + Environment.NewLine;
                    
                    //richTextBox1.SelectionStart = richTextBox1.Text.Length;
                    //richTextBox1.SelectionLength = 0;
                    //richTextBox1.ScrollToCaret();
                },
                ProgressChanged = e =>
                {
                    SetWorkingMessage("Import in progress: "+e.ProgressPercentage.ToString()+"% imported.");
                    
                   /* //SetTextBox1();
                    if (mtextView == "")
                        MessageBox.Show("textView Choice not found");
                    if (mtextView == "📙 ALL")
                    {
                        richTextBox1.Text = richTextBoxAll.Text;
                    }
                    else if (mtextView == "✓ SUCCESS")
                    {
                        richTextBox1.Text = richTextBoxImported.Text;
                    }
                    else if (mtextView == "❌ ERRORS")
                    {
                        richTextBox1.Text = richTextBoxErrors.Text;
                    }
                    else if (mtextView == "⚠ WARNINGS")
                    {
                        richTextBox1.Text = richTextBoxWarning.Text;
                    }*/
                },
                PostWorkCallBack = e =>
                {
                    // This code is executed in the main thread
                    
                    if (textView.SelectedItem.ToString() == "📙 ALL")
                    {
                        richTextBox1.Text = richTextBoxAll.Text;
                    }
                    else if (textView.SelectedItem.ToString() == "✓ SUCCESS")
                    {
                        richTextBox1.Text = richTextBoxImported.Text;
                    }
                    else if (textView.SelectedItem.ToString() == "❌ ERRORS")
                    {
                        richTextBox1.Text = richTextBoxErrors.Text;
                    }
                    else if (textView.SelectedItem.ToString() == "⚠ WARNINGS")
                    {
                        richTextBox1.Text = richTextBoxWarning.Text;
                    }
                }
            });

        }

    }
}
