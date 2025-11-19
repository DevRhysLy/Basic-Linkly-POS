using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using PCEFTPOS.EFTClient.IPInterface;

namespace POS_GUI_Demo
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new AmountForm());
        }
    }

    // ================================================================
    //  MAIN GUI FORM
    // ================================================================
    public class AmountForm : Form
    {
        TextBox amountTextBox;
        Button submitButton;
        Button submitButton2;
        ListBox statusList;
        ComboBox txnTypeBox;

        public AmountForm()
        {
            this.Text = "bestPOS";
            this.Width = 460;
            this.Height = 420;

            Label label = new Label()
            {
                Text = "Enter Amount:",
                Top = 20,
                Left = 20,
            };

            Label typeLabel = new Label()
            {
                Text = "Transaction Type:",
                Top = 80,
                Left = 20,
            };

            amountTextBox = new TextBox()
            {
                Top = 45,
                Left = 20,
                Width = 200,
            };

            submitButton = new Button()
            {
                Text = "Submit Transaction",
                Top = 80,
                Left = 20,
                Width = 200,
            };

            txnTypeBox = new ComboBox()
            {
                Top = 105,
                Left = 20,
                Width = 200,
                DropDownStyle = ComboBoxStyle.DropDownList,
            };

            txnTypeBox.Items.Add("Purchase");
            txnTypeBox.Items.Add("Refund");
            txnTypeBox.SelectedIndex = 0; // default is purchase

            statusList = new ListBox()
            {
                Top = 130,
                Left = 20,
                Width = 400,
                Height = 230,
            };

            submitButton.Click += SubmitButton_Click;

            Controls.Add(label);
            Controls.Add(amountTextBox);
            Controls.Add(submitButton);
            Controls.Add(typeLabel);
            Controls.Add(txnTypeBox);
            Controls.Add(statusList);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            Task.Run(() => RunStartupLogon());
        }

        private void RunStartupLogon()
        {
            UpdateStatus("Running startup logon...");

            try
            {
                var eft = new EFTClientIP()
                {
                    HostName = "127.0.0.1",
                    HostPort = 2011,
                    UseSSL = false,
                };

                if (!eft.Connect())
                {
                    UpdateStatus("Startup logon failed: Cannot connect.");
                    return;
                }

                UpdateStatus("Connected for logon...");

                // var req = new EFTLogonRequest();

                if (!eft.DoLogon())
                {
                    UpdateStatus("Startup logon failed.");
                    return;
                }

                UpdateStatus("Startup Logon Successful.");
                eft.Disconnect();
                eft.Dispose();
            }
            catch (Exception ex)
            {
                UpdateStatus("Startup Logon ERROR: " + ex.Message);
            }
        }

        private void SubmitButton_Click(object sender, EventArgs e)
        {
            if (!decimal.TryParse(amountTextBox.Text, out decimal amount))
            {
                MessageBox.Show("Invalid amount.");
                return;
            }
            string txnType = txnTypeBox.SelectedItem.ToString();

            statusList.Items.Clear();
            statusList.Items.Add($"Starting {txnType}...");

            // Create demo object & run in background
            var demo = new EFTClientIPDemo(amount, txnType, UpdateStatus);
            Task.Run(() => demo.Run());
        }

        // Thread-safe update from EFTClientIPDemo
        private void UpdateStatus(string msg)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(UpdateStatus), msg);
                return;
            }

            statusList.Items.Add(msg);
        }
    }

    public class EFTClientIPDemo
    {
        private decimal _amount;
        private readonly Action<string> _updateUI;
        private string _txnType;
        private readonly ManualResetEvent txnFired = new ManualResetEvent(false);

        public EFTClientIPDemo(decimal amount, string txnType, Action<string> uiCallback)
        {
            _amount = amount;
            _updateUI = uiCallback;
            _txnType = txnType;
        }

        public void Run()
        {
            try
            {
                var eft = new EFTClientIP()
                {
                    HostName = "127.0.0.1",
                    HostPort = 2011,
                    UseSSL = false,
                };

                eft.OnTransaction += Eft_OnTransaction;
                eft.OnReceipt += Eft_OnReceipt;
                eft.OnTerminated += Eft_OnTerminated;

                _updateUI("Connecting to EFT-Client...");

                if (!eft.Connect())
                {
                    _updateUI("Connection failed.");
                    return;
                }

                _updateUI("Connected.");

                TransactionType type =
                    _txnType == "Refund" ? TransactionType.Refund : TransactionType.PurchaseCash;
                _updateUI($"Sending {_txnType} for: {_amount:C}");

                var req = new EFTTransactionRequest()
                {
                    TxnType = type,
                    TxnRef = DateTime.Now.ToString("yyMMddHHmmssfff"),
                    AmtPurchase = _amount,
                    AmtCash = 0.00M,
                    ReceiptAutoPrint = ReceiptPrintModeType.POSPrinter,
                    Application = TerminalApplication.EFTPOS,
                };

                if (!eft.DoTransaction(req))
                {
                    _updateUI("Failed to send transaction.");
                    return;
                }

                _updateUI("Waiting for EFTPOS response...");

                txnFired.WaitOne();

                _updateUI("Disconnecting...");
                eft.Disconnect();
                eft.Dispose();
            }
            catch (Exception ex)
            {
                _updateUI($"ERROR: {ex.Message}");
            }
        }

        private void Eft_OnTransaction(object sender, EFTEventArgs<EFTTransactionResponse> e)
        {
            var result = e.Response.Success ? "SUCCESS" : "FAILED";
            _updateUI($"Transaction Result: {result}");

            txnFired.Set();
        }

        private void Eft_OnReceipt(object sender, EFTEventArgs<EFTReceiptResponse> e)
        {
            _updateUI($"----- Receipt ({e.Response.Type}) -----");
            foreach (var line in e.Response.ReceiptText)
                _updateUI(line);
        }

        private void Eft_OnTerminated(object sender, SocketEventArgs e)
        {
            _updateUI("Connection Terminated.");
            txnFired.Set();
        }
    }
}
