using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using RezaB.Radius.PacketStructure;

namespace RezaB.Radius.DAE.TestUnit
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();

            // request attributes
            {
                var items = Enum.GetNames(typeof(AttributeType))
                  .Select(item => new { Value = (int)Enum.Parse(typeof(AttributeType), item), Key = item })
                  .ToArray();

                RequestAttributeTypeCombobox.DisplayMember = "Key";
                RequestAttributeTypeCombobox.ValueMember = "Value";
                RequestAttributeTypeCombobox.DataSource = items;
            }
            // vendor attributes
            {
                var items = Enum.GetNames(typeof(PacketStructure.Vendors.MikrotikAttribute.Attributes))
                .Select(item => new { Value = (int)Enum.Parse(typeof(PacketStructure.Vendors.MikrotikAttribute.Attributes), item), Key = item })
                .ToArray();

                MikrotikAttributeTypeCombobox.DisplayMember = "Key";
                MikrotikAttributeTypeCombobox.ValueMember = "Value";
                MikrotikAttributeTypeCombobox.DataSource = items;
            }
            // message types
            {
                var items = Enum.GetNames(typeof(MessageTypes))
                  .Select(item => new { Value = (int)Enum.Parse(typeof(MessageTypes), item), Key = item })
                  .ToArray();

                RequestMessageTypeCombobox.DisplayMember = "Key";
                RequestMessageTypeCombobox.ValueMember = "Value";
                RequestMessageTypeCombobox.DataSource = items;
            }
        }

        private void WriteLog(string logText)
        {
            ResponseLogTextbox.AppendText($"{logText}{Environment.NewLine}");
            ResponseLogTextbox.SelectionStart = ResponseLogTextbox.Text.Length;
            ResponseLogTextbox.ScrollToCaret();
        }

        private void RequestAttributeTypeCombobox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if ((int)RequestAttributeTypeCombobox.SelectedValue == (int)AttributeType.VendorSpecific)
            {
                VendorAttributesPanel.Visible = true;
            }
            else
            {
                VendorAttributesPanel.Visible = false;
            }
        }

        private void RequestAttributesListbox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                if (RequestAttributesListbox.SelectedItem != null)
                {
                    RequestAttributesListbox.Items.Remove(RequestAttributesListbox.SelectedItem);
                }
            }
        }

        private void RequestAttributeAddButton_Click(object sender, EventArgs e)
        {
            var item = new RadiusAttributeListItem()
            {
                Type = (AttributeType)RequestAttributeTypeCombobox.SelectedValue,
                Value = RequestAttributeValueTextbox.Text,
                MikrotikType = ((int)RequestAttributeTypeCombobox.SelectedValue == (int)AttributeType.VendorSpecific) ? (PacketStructure.Vendors.MikrotikAttribute.Attributes?)(int)MikrotikAttributeTypeCombobox.SelectedValue : null
            };

            if (string.IsNullOrWhiteSpace(item.Value))
            {
                MessageBox.Show("Invalid input.");
            }

            RequestAttributesListbox.Items.Add(item);
        }

        private void SendRequestButton_Click(object sender, EventArgs e)
        {
            var bindedIP = !string.IsNullOrWhiteSpace(BindedLocalIPTextbox.Text) ? IPAddress.Parse(BindedLocalIPTextbox.Text) : null;
            var requestPacket = new DynamicAuthorizationExtentionPacket((MessageTypes)(int)RequestMessageTypeCombobox.SelectedValue, RequestAttributesListbox.Items.Cast<RadiusAttributeListItem>().Select(item => item.MikrotikType.HasValue ? new RezaB.Radius.PacketStructure.Vendors.MikrotikAttribute(item.MikrotikType.Value, item.Value) : new RadiusAttribute(item.Type, item.Value)));
            using (var client = new DynamicAuthorizationClient((int)BindedLocalPortNumeric.Value, 3000, bindedIP))
            {
                var DASEndPoint = new IPEndPoint(IPAddress.Parse(DASIPTextbox.Text), (int)DASPortNumeric.Value);
                WriteLog(requestPacket.GetLog().ToString());
                var responsePacket = client.Send(DASEndPoint, requestPacket, SecretTextbox.Text);
                WriteLog(responsePacket.GetLog().ToString());
            }
        }
    }
}
