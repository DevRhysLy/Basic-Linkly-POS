using System;
using System.Drawing;
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
        Button getLastTransactionButton;
        ListBox statusList;
        ComboBox txnTypeBox;

        public AmountForm()
        {
            this.Text = "perfectPOS";
            this.Width = 500;
            this.Height = 500;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Segoe UI", 10); // modern font

            int leftMargin = 20;
            int topMargin = 20;
            int spacingY = 10;
            int controlWidth = 200;
            int buttonHeight = 35;
            Color buttonColor = Color.LightBlue;

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
                Top = label.Bottom + spacingY,
                Left = leftMargin,
                Width = controlWidth,
            };

            // Transaction type label and combobox
            Label typeLabel = new Label()
            {
                Text = "Transaction Type:",
                Top = amountTextBox.Bottom + spacingY * 2,
                Left = leftMargin,
                AutoSize = true,
            };

            txnTypeBox = new ComboBox()
            {
                Top = typeLabel.Bottom + spacingY,
                Left = leftMargin,
                Width = controlWidth,
                DropDownStyle = ComboBoxStyle.DropDownList,
            };
            txnTypeBox.Items.Add("Purchase");
            txnTypeBox.Items.Add("Refund");
            txnTypeBox.SelectedIndex = 0;

            // Buttons row 1
            submitButton = new Button()
            {
                Text = "Submit Transaction",
                Top = txnTypeBox.Bottom + spacingY * 2,
                Left = leftMargin,
                Width = controlWidth,
                Height = buttonHeight,
                BackColor = buttonColor,
                FlatStyle = FlatStyle.Flat,
            };

            // Buttons row 2
            settlementButton = new Button()
            {
                Text = "Open Settlement",
                Top = submitButton.Bottom + spacingY,
                Left = leftMargin,
                Width = (controlWidth - spacingY) / 2,
                Height = buttonHeight,
                BackColor = buttonColor,
                FlatStyle = FlatStyle.Flat,
            };

            controlPanelButton = new Button()
            {
                Text = "Open Control Panel",
                Top = submitButton.Bottom + spacingY,
                Left = settlementButton.Right + spacingY,
                Width = (controlWidth - spacingY) / 2,
                Height = buttonHeight,
                BackColor = buttonColor,
                FlatStyle = FlatStyle.Flat,
            };

            // Buttons row 3 - Get Last Transaction
            getLastTransactionButton = new Button()
            {
                Text = "Get Last Transaction",
                Top = controlPanelButton.Bottom + spacingY,
                Left = leftMargin,
                Width = controlWidth,
                Height = buttonHeight,
                BackColor = buttonColor,
                FlatStyle = FlatStyle.Flat,
            };

            // Status ListBox
            statusList = new ListBox()
            {
                Top = getLastTransactionButton.Bottom + spacingY * 2,
                Left = leftMargin,
                Width = this.ClientSize.Width - leftMargin * 2,
                Height = 200,
            };

            submitButton.Click += SubmitButton_Click;
            settlementButton.Click += SettlementButton_Click;
            controlPanelButton.Click += ControlPanelButton_Click;
            getLastTransactionButton.Click += GetLastTransactionButton_Click;

            Controls.Add(label);
            Controls.Add(amountTextBox);
            Controls.Add(typeLabel);
            Controls.Add(txnTypeBox);
            Controls.Add(submitButton);
            Controls.Add(settlementButton);
            Controls.Add(controlPanelButton);
            Controls.Add(getLastTransactionButton);
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

                // if (!eft.DoLogon())
                // {
                //     UpdateStatus("Startup logon failed.");
                //     return;
                // }

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

        private void GetLastTransactionButton_Click(object sender, EventArgs e)
        {
            statusList.Items.Clear();
            UpdateStatus("Retrieving last transaction...");

            Task.Run(() =>
            {
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
                        UpdateStatus("Failed to connect to EFT-Client.");
                        return;
                    }

                    var req = new EFTGetLastTransactionRequest();

                    eft.OnGetLastTransaction += (s, ea) =>
                    {
                        var lastTxn = ea.Response;
                        UpdateStatus(
                            $"Last Txn Type: {lastTxn.TxnType}, Amount: {lastTxn.AmtPurchase:C}, "
                                + $"Success: {lastTxn.LastTransactionSuccess}"
                        );

                        eft.Disconnect();
                        eft.Dispose();
                    };

                    if (!eft.DoRequest(req))
                    {
                        UpdateStatus("Failed to send Get Last request.");
                        eft.Disconnect();
                        eft.Dispose();
                        return;
                    }
                }
                catch (Exception ex)
                {
                    UpdateStatus("Error retrieving last transaction: " + ex.Message);
                }
            });
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
        private EFTClientIP _eft;

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
                _eft = new EFTClientIP()
                {
                    HostName = "127.0.0.1",
                    HostPort = 2011,
                    UseSSL = false,
                };

                _eft.OnTransaction += Eft_OnTransaction;
                _eft.OnReceipt += Eft_OnReceipt;
                _eft.OnTerminated += Eft_OnTerminated;

                _updateUI("Connecting to EFT-Client...");

                if (!_eft.Connect())
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

                if (!_eft.DoTransaction(req))
                {
                    _updateUI("Failed to send transaction.");
                    return;
                }

                _updateUI("Waiting for EFTPOS response (30s timeout)...");

                bool fired = txnFired.WaitOne(TimeSpan.FromSeconds(30));

                if (!fired)
                {
                    _updateUI("Timeout reached! Sending CANCEL to terminal...");

                    try
                    {
                        _eft.DoSendKey(EFTPOSKey.OkCancel);
                        _updateUI("Cancel sent. Waiting for cancel response (10s)...");
                        bool cancelFired = txnFired.WaitOne(TimeSpan.FromSeconds(10));
                        if (!cancelFired)
                        {
                            _updateUI("Cancel failed or no response from terminal.");
                        }
                    }
                    catch (Exception ce)
                    {
                        _updateUI("Cancel ERROR: " + ce.Message);
                    }
                }

                _updateUI("Disconnecting...");
                _eft.Disconnect();
                _eft.Dispose();
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
