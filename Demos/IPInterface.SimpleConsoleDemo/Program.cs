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
        Button settlementButton;
        Button controlPanelButton;
        ListBox statusList;
        ComboBox txnTypeBox;

        public AmountForm()
        {
            this.Text = "perfectPOS";
            this.Width = 500;
            this.Height = 500;

            int leftMargin = 20;
            int topMargin = 20;
            int spacing = 10;

            // Amount label and textbox
            Label label = new Label()
            {
                Text = "Enter Amount:",
                Top = topMargin,
                Left = leftMargin,
                AutoSize = true,
            };

            amountTextBox = new TextBox()
            {
                Top = label.Bottom + spacing,
                Left = leftMargin,
                Width = 200,
            };

            // Transaction type label and combobox
            Label typeLabel = new Label()
            {
                Text = "Transaction Type:",
                Top = amountTextBox.Bottom + spacing * 2,
                Left = leftMargin,
                AutoSize = true,
            };

            txnTypeBox = new ComboBox()
            {
                Top = typeLabel.Bottom + spacing,
                Left = leftMargin,
                Width = 200,
                DropDownStyle = ComboBoxStyle.DropDownList,
            };
            txnTypeBox.Items.Add("Purchase");
            txnTypeBox.Items.Add("Refund");
            txnTypeBox.SelectedIndex = 0;

            // Buttons row 1
            submitButton = new Button()
            {
                Text = "Submit Transaction",
                Top = txnTypeBox.Bottom + spacing * 2,
                Left = leftMargin,
                Width = 200,
            };

            // Buttons row 2
            settlementButton = new Button()
            {
                Text = "Open Settlement",
                Top = submitButton.Bottom + spacing,
                Left = leftMargin,
                Width = 150,
            };

            controlPanelButton = new Button()
            {
                Text = "Open Control Panel",
                Top = submitButton.Bottom + spacing,
                Left = settlementButton.Left + settlementButton.Width + spacing,
                Width = 150,
            };

            // Status ListBox
            statusList = new ListBox()
            {
                Top = controlPanelButton.Bottom + spacing * 2,
                Left = leftMargin,
                Width = this.ClientSize.Width - 40,
                Height = 200,
            };

            // Wire up events
            submitButton.Click += SubmitButton_Click;
            settlementButton.Click += SettlementButton_Click;
            controlPanelButton.Click += ControlPanelButton_Click;

            // Add controls
            Controls.Add(label);
            Controls.Add(amountTextBox);
            Controls.Add(typeLabel);
            Controls.Add(txnTypeBox);
            Controls.Add(submitButton);
            Controls.Add(settlementButton);
            Controls.Add(controlPanelButton);
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

        private async void ControlPanelButton_Click(object sender, EventArgs e)
        {
            statusList.Items.Clear();
            UpdateStatus("Opening Linkly Control Panel...");

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
                    UpdateStatus("Failed to connect to Linkly Client.");
                    return;
                }

                UpdateStatus("Connected. Sending Control Panel request...");

                var req = new EFTControlPanelRequest() { ControlPanelType = ControlPanelType.Full };

                if (!eft.DoDisplayControlPanel(req))
                {
                    UpdateStatus("Failed to send control panel request.");
                    return;
                }

                UpdateStatus("Control panel opened.");
                await Task.Delay(500);

                eft.Disconnect();
                eft.Dispose();
            }
            catch (Exception ex)
            {
                UpdateStatus("Error opening control panel: " + ex.Message);
            }
        }

        private async void SettlementButton_Click(object sender, EventArgs e)
        {
            statusList.Items.Clear();
            UpdateStatus("Opening Linkly Settlement Panel...");

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
                    UpdateStatus("Failed to connect to Linkly Client.");
                    return;
                }

                UpdateStatus("Connected. Sending Control Panel request...");

                var req = new EFTControlPanelRequest()
                {
                    ControlPanelType = ControlPanelType.Settlement,
                };

                if (!eft.DoDisplayControlPanel(req))
                {
                    UpdateStatus("Failed to send control panel request.");
                    return;
                }

                UpdateStatus("Settlement panel opened.");
                await Task.Delay(500);

                eft.Disconnect();
                eft.Dispose();
            }
            catch (Exception ex)
            {
                UpdateStatus("Error opening control panel: " + ex.Message);
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

            var demo = new EFTClientIPDemo(amount, txnType, UpdateStatus);
            Task.Run(() => demo.Run());
        }

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
