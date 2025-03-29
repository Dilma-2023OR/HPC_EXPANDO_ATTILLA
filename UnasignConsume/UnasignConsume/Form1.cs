using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using UnasignConsume.Class;
using UnasignConsume.RuncardServices;

namespace UnasignConsume
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        //Config Connection
        INIFile localConfig = new INIFile(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\UnasignConsume\config.ini");

        //Runcard Connection
        runcard_wsdlPortTypeClient client = new runcard_wsdlPortTypeClient("runcard_wsdlPort");
        string msg = string.Empty;
        unitBOM[] getBOM = null;
        unitBOMitem[] getBomItems = null;
        int error = 0;

        //List
        List<string> bomList = new List<string>();

        //Config Data
        string warehouseBin = string.Empty;
        string warehouseLoc = string.Empty;
        string partClass = string.Empty;
        string machineId = string.Empty;
        string opcode = string.Empty;
        string seqnum = string.Empty;

        int bomCount = 0;
        int contador = 0;
        int errExpendor = 0;


        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                if (!Directory.Exists(Path.GetDirectoryName(localConfig.FilePath)))
                {
                    //Config Directory
                    Directory.CreateDirectory(Path.GetDirectoryName(localConfig.FilePath));
                    File.Copy(Directory.GetCurrentDirectory() + "\\config.ini", localConfig.FilePath);
                }

                if (!Directory.Exists(Path.GetDirectoryName(localConfig.FilePath)))
                {
                    //Config Directory
                    Directory.CreateDirectory(Path.GetDirectoryName(localConfig.FilePath));
                    File.Copy(Directory.GetCurrentDirectory() + "\\config.ini", localConfig.FilePath);
                }
                dataGridView1.DefaultCellStyle.Font = new Font("Franklin Gothic Medium Cond", 13.8F);
                dataGridView1.DefaultCellStyle.ForeColor = Color.Black;
                dataGridView1.ColumnHeadersDefaultCellStyle.Font = new Font("Ebrima", 19.8000011F, FontStyle.Bold);
                dataGridView1.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;

                dataGridView1.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;

                warehouseBin = localConfig.Read("RUNCARD_INFO", "warehouseBin");
                warehouseLoc = localConfig.Read("RUNCARD_INFO", "warehouseLoc");
                partClass = localConfig.Read("RUNCARD_INFO", "partClass");
                machineId = localConfig.Read("RUNCARD_INFO", "machineID");
                opcode = localConfig.Read("RUNCARD_INFO", "opcode");
                seqnum = localConfig.Read("RUNCARD_INFO", "seqnum");

                //crear encabezados del datagridview
                //dataGridView1.Dock = DockStyle.Fill;

                DataGridViewTextBoxColumn tbId = new DataGridViewTextBoxColumn();
                tbId.HeaderText = "ID";
                tbId.Name = "ID";
                tbId.FillWeight = 50;
                tbId.Width = 144;

                dataGridView1.Columns.Add(tbId);

                DataGridViewTextBoxColumn tbMaterial = new DataGridViewTextBoxColumn();
                tbMaterial.HeaderText = "Material";
                tbMaterial.Name = "Material";
                tbMaterial.FillWeight = 100;
                tbMaterial.Width = 287;

                dataGridView1.Columns.Add(tbMaterial);

                DataGridViewTextBoxColumn tbRev = new DataGridViewTextBoxColumn();
                tbRev.HeaderText = "Rev";
                tbRev.Name = "Rev";
                tbRev.FillWeight = 50;
                tbRev.Width = 144;

                dataGridView1.Columns.Add(tbRev);

                DataGridViewTextBoxColumn tbUniqueId = new DataGridViewTextBoxColumn();
                tbUniqueId.HeaderText = "Unique Id";
                tbUniqueId.Name = "UniqueId";
                tbUniqueId.FillWeight = 100;
                tbUniqueId.Width = 288;

                dataGridView1.Columns.Add(tbUniqueId);

                DataGridViewTextBoxColumn tbCantidad = new DataGridViewTextBoxColumn();
                tbCantidad.HeaderText = "Cantidad";
                tbCantidad.Name = "Cantidad";
                tbCantidad.FillWeight = 55;
                tbCantidad.Width = 158;

                dataGridView1.Columns.Add(tbCantidad);

                //Temporal Data
                string dBMsg = string.Empty;
                int dBError = 0;

                //Data Base Connection 
                DBConnection dB = new DBConnection();
                DataTable dtResult = new DataTable();

                dB.dataBase = "datasource=mlxgumvlptfrd01.molex.com;port=3306;username=ftest;password=Ftest123#;database=runcard_tempflex;";
                dB.query = "SELECT partnum FROM runcard_tempflex.prod_master_config"
                         + " INNER JOIN runcard_tempflex.prod_step_config ON runcard_tempflex.prod_step_config.prr_config_id = runcard_tempflex.prod_master_config.prr_config_id AND runcard_tempflex.prod_step_config.prr_config_rev = runcard_tempflex.prod_master_config.prr_config_rev"
                         + " WHERE status = \"ACTIVE\" AND opcode = \"" + opcode + "\" AND part_class IN ('" + partClass + "');";
                var dBResult = dB.getData(out dBMsg, out dBError);

                if (dBError != 0)
                {
                    //Control Adjust
                    CboPartNum.Enabled = false;

                    //Feedback
                    Message message = new Message(dBMsg);
                    message.ShowDialog();
                    return;
                }

                //Fill Data Table
                dBResult.Fill(dtResult);
                foreach (DataRow row in dtResult.Rows)
                {
                    if (!CboPartNum.Items.Contains(row.ItemArray[0]))
                        CboPartNum.Items.Add(row.ItemArray[0]);
                }


            }
            catch (Exception ex)
            {
                //Control Adjust
                CboPartNum.Enabled = false;

                //Feedback
                Message message = new Message("Error al obtener la configuración");
                message.ShowDialog();

                //Log
                File.AppendAllText(Directory.GetCurrentDirectory() + @"\errorLog.txt", DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss") + ",Error al obtener la configuración:" + ex.Message + "\n");
            }
        }

        private void CboPartNum_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (CboPartNum.Text != string.Empty)
            {
                try
                {
                    //Clear Save Data
                    CboWorkOrder.Items.Clear();

                    //Get Work Orders
                    var getWorkOrders = client.getAvailableWorkOrders(CboPartNum.Text, "", out error, out msg);

                    foreach (workOrderItem order in getWorkOrders)
                        if (!CboWorkOrder.Items.Contains(order.workorder))
                            CboWorkOrder.Items.Add(order.workorder);

                    //Control Adjust
                    CboWorkOrder.Enabled = true;
                }
                catch (Exception ex)
                {
                    //Feedback
                    Message message = new Message("Error al obtener las ordenes");
                    message.ShowDialog();

                    //Log
                    File.AppendAllText(Directory.GetCurrentDirectory() + @"\errorLog.txt", DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss") + ",Error al obtener las ordenes:" + ex.Message + "\n");
                }
            }
        }

        private void tbSerial_KeyDown(object sender, KeyEventArgs e)
        {
            if (CboWorkOrder.Text != string.Empty)
            {
                //Control Adjust
                dataGridView1.Controls.Clear();

                try
                {
                    //Get BOM
                    getBomItems = client.getUnitBOMItems(tbSerial.Text, CboWorkOrder.Text, Convert.ToInt32(seqnum), out int error, out string msg);
                    //getBOM = client.getUnitBOMConsumption(CboWorkOrder.Text, seqnum, out error, out msg);

                    if (getBomItems.Length == 0)
                    {
                        Message message = new Message("El serial actual no cuenta con BOM");
                        message.ShowDialog();

                        //Log
                        File.AppendAllText(Directory.GetCurrentDirectory() + @"\errorLog.txt", DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss") + ",La orden actual no cuenta con BOM\n");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    //Retroalimentación
                    Message message = new Message("Error al obtener el BOM");
                    message.ShowDialog();

                    //Log
                    File.AppendAllText(Directory.GetCurrentDirectory() + @"\errorLog.txt", DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss") + ",Error al obtener el BOM:" + ex.Message + "\n");
                    return;
                }

                //crearDatagrid();
            }
        }
    } 
}
