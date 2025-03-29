using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using System.Windows.Forms;
using HPC_EXPANDO_ATTILA.Class;
using HPC_EXPANDO_ATTILA.RuncardServices;
namespace HPC_EXPANDO_ATTILA
{
    public partial class FrmMain : Form
    {
        public FrmMain()
        {
            InitializeComponent();
        }
        //Config Connection
        INIFile localConfig = new INIFile(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\HPC Expando Atilla\config.ini");

        //Runcard Connection
        runcard_wsdlPortTypeClient client = new runcard_wsdlPortTypeClient("runcard_wsdlPort");
        string msg = string.Empty;
        unitBOM[] getBOM = null;
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

        private void FrmMain_Load(object sender, EventArgs e)
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

                lblOpcode.Text = opcode;
                lblMessage.Text = "";

                //crear encabezados del datagridview
                dataGridView1.Dock = DockStyle.Fill;

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
                    cBoxPartNum.Enabled = false;

                    //Feedback
                    Message message = new Message(dBMsg);
                    message.ShowDialog();
                    return;
                }

                //Fill Data Table
                dBResult.Fill(dtResult);
                foreach (DataRow row in dtResult.Rows)
                {
                    if (!cBoxPartNum.Items.Contains(row.ItemArray[0]))
                        cBoxPartNum.Items.Add(row.ItemArray[0]);
                }
            }
            catch (Exception ex)
            {
                //Control Adjust
                cBoxPartNum.Enabled = false;

                //Feedback
                Message message = new Message("Error al obtener la configuración");
                message.ShowDialog();

                //Log
                File.AppendAllText(Directory.GetCurrentDirectory() + @"\errorLog.txt", DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss") + ",Error al obtener la configuración:" + ex.Message + "\n");
            }
        }

        private void cBoxPartNum_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cBoxPartNum.Text != string.Empty)
            {
                try
                {
                    //Clear Save Data
                    cBoxWorkOrder.Items.Clear();

                    //Get Work Orders
                    var getWorkOrders = client.getAvailableWorkOrders(cBoxPartNum.Text, "", out error, out msg);

                    foreach (workOrderItem order in getWorkOrders)
                        if (!cBoxWorkOrder.Items.Contains(order.workorder))
                            cBoxWorkOrder.Items.Add(order.workorder);

                    //Control Adjust
                    cBoxWorkOrder.Enabled = true;
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

        private void cBoxWorkOrder_SelectedIndexChanged(object sender, EventArgs e)
        {
            
            if (cBoxWorkOrder.Text != string.Empty)
            {
                //Control Adjust
                dataGridView1.Controls.Clear();

                try
                {
                    //Get BOM
                    getBOM = client.getUnitBOMConsumption(cBoxWorkOrder.Text, seqnum, out error, out msg);

                    if (getBOM.Length == 0)
                    {
                        Message message = new Message("La orden actual no cuenta con BOM");
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

                crearDatagrid();
            }
        }

        public void crearDatagrid() {

            dataGridView1.Rows.Clear();

            InventoryItem[] fetchSeriales = null;
            string numSerie = string.Empty;
            string moddate = string.Empty;
            int step = 0;

            //Response
            int response = 0;
            foreach (unitBOM item in getBOM)
            {
                if (item.alt_for_item == 0)
                {
                    try
                    {
                        fetchSeriales = client.fetchInventoryItems("", "", "21116613XO", "", "", "", 0, "", "", out error, out msg);
                        numSerie = fetchSeriales[0].serial;
                        moddate = fetchSeriales[0].moddate;


                    }
                    catch (Exception ex){
                        //Feedback
                        Message message = new Message("Error al consultar el status de la work order " + cBoxWorkOrder.Text);
                        message.ShowDialog();

                        //Log
                        File.AppendAllText(Directory.GetCurrentDirectory() + @"\errorLog.txt", DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss") + ",Error al consultar el status de la work order:  " + cBoxWorkOrder.Text + ":" + ex.Message + "\n");

                        //Response
                        response = -1;
                        return;
                    }
                    var datosFiltrados = fetchSeriales.Where(d => (d.status == "COMPLETE" ) | (d.status == "AVAILABLE") | (d.status == "IN PROGRESS")).ToList();
                    var datoMasAntiguo = datosFiltrados.OrderBy(d => d.moddate).FirstOrDefault();
                    int cantidad = 1;

                    if (datoMasAntiguo != null)
                    {
                        DataGridViewRow row1 = new DataGridViewRow();
                        row1.Cells.Add(new DataGridViewTextBoxCell { Value = cantidad });
                        row1.Cells.Add(new DataGridViewTextBoxCell { Value = item.partnum});
                        row1.Cells.Add(new DataGridViewTextBoxCell { Value = item.partrev });
                        row1.Cells.Add(new DataGridViewTextBoxCell { Value = datoMasAntiguo.serial });
                        row1.Cells.Add(new DataGridViewTextBoxCell { Value = datoMasAntiguo.qty });

                        dataGridView1.Rows.Add(row1);

                        dataGridView1.AllowUserToAddRows = false;
                    }

                    //habilitar barras de desplazamiento si el contenido excede el tamaño del datagridview

                    cBoxWorkOrder.Enabled = false;
                    cBoxPartNum.Enabled = false;
                    tBoxLabelA.Enabled = true;
                    tBoxLabelA.Clear();
                    tBoxLabelA.Focus();
                    dataGridView1.ResumeLayout();
                    btnChange.Enabled = true;

                    dataGridView1.ScrollBars = ScrollBars.Both;
                    dataGridView1.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
                }
            }
        }

        private void tBoxLabelA_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter & tBoxLabelA.Text != string.Empty)
            {
                string scanInfo = "";
                errExpendor = 0;

                foreach (char c in tBoxLabelA.Text)
                {
                    if (!char.IsControl(c))
                    {
                        scanInfo = scanInfo + c;
                    }
                }

                string serial1 = tBoxLabelA.Text.Substring(1);

                //temporal Data
                int response = 0;

                //Register Unit 
                serialRegister(serial1, out response);

                if (response != 0)
                {
                    //Control Adjust
                    tBoxLabelA.Clear();
                    tBoxLabelA.Focus();
                    return;
                }

                serialTransaction(serial1, out response);

                if (response != 0)
                {
                    //Control Adjust
                    tBoxLabelA.Clear();
                    tBoxLabelA.Focus();
                    return;
                }

                //Control Adjust
                tBoxLabelA.Enabled = true;
                tBoxLabelA.Clear();
                tBoxLabelA.Focus();
            }
        }

        private void serialRegister(string serial, out int response)
        {
            int register = -1;
            response = 0;
            int qty = 0;

            try
            {
                qty = 1;

                register = client.registerUnitToWorkOrder(cBoxWorkOrder.Text, serial, qty, "", "", "WIP", "PRODUCTION FLOOR", "ftest", out string msg);

                if (error != 0)
                {
                    //Retroalimentación
                    Message message = new Message("Error al registrar el serial " + serial);
                    message.ShowDialog();

                    //Log
                    File.AppendAllText(Directory.GetCurrentDirectory() + @"\errorLog.txt", DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss") + ",Error al registar el serial " + serial + ":" + msg + "\n");

                    //Response
                    response = -1;
                    return;
                }
            }
            catch (Exception ex) {
                //Feedback
                Message message = new Message("Error al registar el serial " + serial);
                message.ShowDialog();

                //Log
                File.AppendAllText(Directory.GetCurrentDirectory() + @"\errorLog.txt", DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss") + ",Error al registar el serial " + serial + ":" + ex.Message + "\n");

                //Response
                response = -1;
            }
        }

        private void serialTransaction(string serial, out int response)
        {
            InventoryItem[] fetchInv = null;
            string workorder = string.Empty;
            string operation = string.Empty;
            string partnum = string.Empty;
            string partrev = string.Empty;
            string status = string.Empty;
            int step = 0;
            //Response 
            response = 0;

            try
            {
                fetchInv = client.fetchInventoryItems(serial, "", "", "", "", "", 0, "", "", out error, out msg);
                workorder = fetchInv[0].workorder;
                operation = fetchInv[0].opcode;
                partnum = fetchInv[0].partnum;
                partrev = fetchInv[0].partrev;
                status = fetchInv[0].status;
                step = fetchInv[0].seqnum;
            }
            catch (Exception ex) {
                //Feedback
                Message message = new Message("Error al consultar el status del serial " + serial);
                message.ShowDialog();

                //Log
                File.AppendAllText(Directory.GetCurrentDirectory() + @"\errorLog.txt", DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss") + ",Error al consultar el status del serial " + serial + ":" + ex.Message + "\n");

                //Response
                response = -1;
                return;
            }

            if (status == "IN QUEUE" & operation == opcode | status == "IN PROGRESS" & operation == opcode)
            {
                // Transaction Item
                transactionItem transItem = new transactionItem();
                transItem.workorder = cBoxWorkOrder.Text;
                transItem.warehouseloc = warehouseLoc;
                transItem.warehousebin = warehouseBin;
                transItem.username = "ftest";
                transItem.machine_id = machineId;
                transItem.transaction = "MOVE";
                transItem.opcode = operation;
                transItem.serial = serial;
                transItem.trans_qty = 1;
                transItem.seqnum = step;
                transItem.comment = "TRANSACCION HECHA POR SISTEMA";

                //Data/BOM Item
                bomItem[] bomData = new bomItem[getBOM.Length];
                dataItem[] inputData = new dataItem[] { };

                //Counter
                int bom = 0;

                string partnum1 = string.Empty;
                string uniqueId = string.Empty;
                int cantidad = 0;
                string rev = string.Empty;

                foreach (DataGridViewRow row in dataGridView1.Rows)
                {
                    uniqueId = row.Cells[3].Value.ToString();
                    partnum1 = row.Cells[1].Value.ToString();
                    rev = row.Cells[2].Value.ToString();

                    cantidad = Convert.ToInt32(row.Cells[4].Value.ToString());

                    bomData[bom] = new bomItem();
                    bomData[bom].item_serial = uniqueId;
                    bomData[bom].item_partnum = partnum1;
                    bomData[bom].item_partrev = rev;

                    foreach (unitBOM part in getBOM)
                    
                        if (partnum1 == part.partnum)
                        {
                            bomData[bom].item_qty = 1;

                            //Por cada pieza del BOM
                            for (int x = 0; x < dataGridView1.Rows.Count; x++)
                            {
                                if (partnum1.Contains(part.partnum))
                                {
                                    cantidad = (cantidad - Convert.ToInt32(part.qty));

                                    if (cantidad <= 0)
                                    {
                                        crearDatagrid();
                                    }
                                    else
                                        dataGridView1.Rows[x].Cells[4].Value = Convert.ToString(cantidad);
                                }
                            }
                            break;
                        }
                    bom++;

                    if (dataGridView1.Rows.Count == 0)
                        break;
                }

                try
                {
                    //Transaction
                    var transaction = client.transactUnit(transItem, inputData, bomData, out msg);

                    //MessageBox.Show(msg);
                    if (!msg.Contains("ADVANCE"))
                    {
                        //Feedback
                        lblMessage.Text = "Pase NO otorgado al serial " + serial;
                        tLayoutMessage.BackColor = Color.Crimson;
                        MostrarMensajeFlotanteNoPass(" NO PASS");

                        //Log
                        File.AppendAllText(Directory.GetCurrentDirectory() + @"\errorLog.txt", DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss") + ",Pase NO otorgado al serial " + serial + ":" + msg + "\n");

                        //Response
                        response = -1;
                        return;
                    }

                    //Feedback
                    lblMessage.Text = "Serial " + serial + " Completado";
                    tLayoutMessage.BackColor = Color.FromArgb(58, 196, 123);
                    MostrarMensajeFlotante("P A S S");

                    //Log
                    File.AppendAllText(Directory.GetCurrentDirectory() + @"\Log.txt", DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss") + "," + msg + "\n");
                }
                catch (Exception ex)
                {
                    //Feedback
                    Message message = new Message("Error al dar el pase al serial " + serial);
                    message.ShowDialog();

                    //Log
                    File.AppendAllText(Directory.GetCurrentDirectory() + @"\errorLog.txt", DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss") + ",Error al dar el pase al serial " + serial + ":" + ex.Message + "\n");
                    //Response
                    response = -1;
                    return;
                }
            }
            else {
                //Get Instructions
                var getInstructions = client.getWorkOrderStepInstructions(cBoxWorkOrder.Text, step.ToString(), out error, out msg);

                //Feedback
                lblMessage.Text = "Serial " + serial + " sin flujo, " + status + ":" + getInstructions.opdesc;
                tLayoutMessage.BackColor = Color.Crimson;

                //Response
                response = -1;
            }
        }

        // Método para mostrar el mensaje flotante gigante
        private void MostrarMensajeFlotante(string mensaje)
        {
            // Crear un formulario emergente flotante
            Form flotanteForm = new Form();
            flotanteForm.FormBorderStyle = FormBorderStyle.None;  // Sin bordes
            flotanteForm.StartPosition = FormStartPosition.CenterScreen;  // Centrado en la pantalla
            flotanteForm.BackColor = Color.Green;  // Fondo verde (puedes cambiar el color)
            flotanteForm.Opacity = 0.9;  // Opacidad para hacerlo semitransparente
            flotanteForm.TopMost = true;  // Asegura que esté sobre otras ventanas
            flotanteForm.Width = 600;  // Ancho de la ventana flotante
            flotanteForm.Height = 200;  // Alto de la ventana flotante

            // Crear un label para mostrar el mensaje
            Label mensajeLabel = new Label();
            mensajeLabel.AutoSize = false;
            mensajeLabel.Size = new Size(flotanteForm.Width, flotanteForm.Height);
            mensajeLabel.Text = mensaje;
            mensajeLabel.Font = new Font("Arial", 48, FontStyle.Bold);  // Tamaño grande de la fuente
            mensajeLabel.ForeColor = Color.White;  // Color de texto blanco
            mensajeLabel.TextAlign = ContentAlignment.MiddleCenter;  // Centrado en el label

            // Añadir el label al formulario flotante
            flotanteForm.Controls.Add(mensajeLabel);

            // Mostrar el mensaje durante 3 segundos y luego cerrar
            flotanteForm.Show();
            Timer timer = new Timer();
            timer.Interval = 3000;  // 3000 milisegundos = 3 segundos
            timer.Tick += (sender, e) =>
            {
                flotanteForm.Close();
                timer.Stop();
            };
            timer.Start();
        }

        private void MostrarMensajeFlotanteNoPass(string mensaje)
        {
            // Crear un formulario emergente flotante
            Form flotanteForm = new Form();
            flotanteForm.FormBorderStyle = FormBorderStyle.None;  // Sin bordes
            flotanteForm.StartPosition = FormStartPosition.CenterScreen;  // Centrado en la pantalla
            flotanteForm.BackColor = Color.Red;  // Fondo verde (puedes cambiar el color)
            flotanteForm.Opacity = 0.9;  // Opacidad para hacerlo semitransparente
            flotanteForm.TopMost = true;  // Asegura que esté sobre otras ventanas
            flotanteForm.Width = 600;  // Ancho de la ventana flotante
            flotanteForm.Height = 200;  // Alto de la ventana flotante

            // Crear un label para mostrar el mensaje
            Label mensajeLabel = new Label();
            mensajeLabel.AutoSize = false;
            mensajeLabel.Size = new Size(flotanteForm.Width, flotanteForm.Height);
            mensajeLabel.Text = mensaje;
            mensajeLabel.Font = new Font("Arial", 48, FontStyle.Bold);  // Tamaño grande de la fuente
            mensajeLabel.ForeColor = Color.White;  // Color de texto blanco
            mensajeLabel.TextAlign = ContentAlignment.MiddleCenter;  // Centrado en el label

            // Añadir el label al formulario flotante
            flotanteForm.Controls.Add(mensajeLabel);

            // Mostrar el mensaje durante 3 segundos y luego cerrar
            flotanteForm.Show();
            Timer timer = new Timer();
            timer.Interval = 3000;  // 3000 milisegundos = 3 segundos
            timer.Tick += (sender, e) =>
            {
                flotanteForm.Close();
                timer.Stop();
            };
            timer.Start();
        }

        private void btnChange_Click(object sender, EventArgs e)
        {
            //Control Adjust
            tLayoutMessage.BackColor = Color.White;
            cBoxWorkOrder.SelectedIndex = -1;
            cBoxPartNum.SelectedIndex = -1;
            cBoxWorkOrder.Enabled = false;
            dataGridView1.Rows.Clear();
            cBoxPartNum.Enabled = true;
            tBoxLabelA.Enabled = false;
            btnChange.Enabled = false;
            lblMessage.Text = "";
            bomList.Clear();
            bomCount = 0;
            contador = 0;
        }

        private void lblMessage_TextChanged(object sender, EventArgs e)
        {
            //Timer Start
            timerTextReset.Start();
        }

        private void timerTextReset_Tick(object sender, EventArgs e)
        {
            //Timer Stop
            timerTextReset.Stop();

            //Control Adjust
            tLayoutMessage.BackColor = Color.White;
            lblMessage.Text = string.Empty;
        }
    }
}
