using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Sender.EXControls
{
    public class StorageMessageListView : ListViewEx
    {

        private List<string> _messages;


        public StorageMessageListView() : base()
        {
            _messages = new List<string>();

            Initialize();
        }

        public StorageMessageListView(string serializableMessagelistPath)
            : this()
        {
            if (!File.Exists(serializableMessagelistPath))
            {
                return;
            }

           
            using (Stream fs = new FileStream(serializableMessagelistPath, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                if (fs.Length == 0)
                {
                    return;
                }

                BinaryFormatter formatter = new BinaryFormatter();

                List<string> messagesObjectGraph = formatter.Deserialize(fs) as List<string>;

                if (messagesObjectGraph != null)
                {
                    _messages = messagesObjectGraph;

                    this.GetSourceListMessages();
                }
            }
        }

        public string MessageSelected
        {
            get { return base.SelectedItems[0].Text; }
        }

        public List<string> Messages 
        {
            get { return _messages; }
        }

        public string this[int index]
        {
            get 
            {
                return base.Items[index].Text;
            }

            set
            {
                base.Items[index].Text = value;
            }
        }

        private void Initialize()
        {
            foreach (ListViewItem msg in base.Items)
            {
                _messages.Add(msg.Text);
            }
        }

        public void Refresh()
        {
            throw new NotImplementedException();
        }

        public void Refresh(List<string> addedMessages)
        {
            foreach (var msg in addedMessages)
            {
                this.AddRow(msg);
            }
        }

        public void GetSourceListMessages()
        {
            Refresh(Messages);
        }

        public void AddNewMessage(string message)
        {
            Messages.Add(message);
            this.AddRow(message);
        }
    }
}
