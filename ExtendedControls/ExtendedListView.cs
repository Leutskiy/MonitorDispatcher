#region Библиотеки платформы .NET Framework

using System;
using System.Drawing;
using System.Collections;
using System.Windows.Forms;
using System.ComponentModel;
using System.Collections.Generic;
using System.Runtime.InteropServices;

#endregion

/* Расширенные элементы управления шаблона Windows Forms */
namespace ExtendedControls
{
    /// <summary>
    /// Расширенный ListView.
    /// </summary>
    public class ExtendedListView : ListView
    {
        #region Private fields

        private Brush _highlightbrush;
        private List<EmbeddedControl> _embeddedControls;
        private HeaderControl hdrCtrl;
        private bool locked;

        #endregion

        #region Interop-Defines

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wPar, IntPtr lPar);

        // ListView messages
        private const int LVM_FIRST					= 0x1000;
        private const int LVM_GETCOLUMNORDERARRAY	= (LVM_FIRST + 59);

        // Windows Messages
        private const int WM_PAINT = 0x000F;

        [DllImport("user32")]
        public static extern bool ShowScrollBar(System.IntPtr hWnd, int wBar, bool bShow);

        private const uint SB_HORZ = 0;
        private const uint SB_VERT = 1;

        #endregion

        #region Nested types

        /// <summary>
        /// Structure to hold an embedded control's info
        /// </summary>
        private struct EmbeddedControl
        {
            public Control Control;
            public int Column;
            public int Row;
            public DockStyle Dock;
            public ListViewItem Item;
        }

        #endregion

        public ExtendedListView()
        {

            _highlightbrush = Brushes.BlueViolet;
            _embeddedControls = new List<EmbeddedControl>();

            hdrCtrl = null;
            locked = true;

            #region Set some properties by default

            this.OwnerDraw = true;

            #endregion


            #region Subscribe on events

            this.DrawItem += new DrawListViewItemEventHandler(this_DrawItem);
            this.DrawColumnHeader += new DrawListViewColumnHeaderEventHandler(this_DrawColumnHeader);
            this.DrawSubItem += new DrawListViewSubItemEventHandler(this_DrawSubItem);

            #endregion
        }

        /// <summary>
        /// Property to turn on and off the ability to size the column headers.
        /// </summary>
        [Category("Behavior"), Description("Prevent sizing of column headers.")]
        public bool LockColumnSize
        {
            get { return locked; }
            set
            {
                locked = value;
            }
        }

        /// <summary>
        /// Retrieve the order in which columns appear
        /// </summary>
        /// <returns>Current display order of column indices</returns>
        protected int[ ] GetColumnOrder()
        {
            IntPtr lPar	= Marshal.AllocHGlobal(Marshal.SizeOf(typeof(int)) * Columns.Count);

            IntPtr res = SendMessage(Handle, LVM_GETCOLUMNORDERARRAY, new IntPtr(Columns.Count), lPar);
            if (res.ToInt32() == 0)	// Something went wrong
            {
                Marshal.FreeHGlobal(lPar);
                return null;
            }

            int	[] order = new int[Columns.Count];
            Marshal.Copy(lPar, order, 0, Columns.Count);

            Marshal.FreeHGlobal(lPar);

            return order;
        }

        /// <summary>
        /// Retrieve the bounds of a ListViewSubItem
        /// </summary>
        /// <param name="Item">The Item containing the SubItem</param>
        /// <param name="SubItem">Index of the SubItem</param>
        /// <returns>Subitem's bounds</returns>
        protected Rectangle GetSubItemBounds(ListViewItem Item, int SubItem)
        {
            Rectangle subItemRect = Rectangle.Empty;

            if (Item == null)
                throw new ArgumentNullException("Item");

            int[] order = GetColumnOrder();
            if (order == null) // No Columns
                return subItemRect;

            if (SubItem >= order.Length)
                throw new IndexOutOfRangeException("SubItem " + SubItem + " out of range");

            // Retrieve the bounds of the entire ListViewItem (all subitems)
            Rectangle lviBounds = Item.GetBounds(ItemBoundsPortion.Entire);
            int	subItemX = lviBounds.Left;

            // Calculate the X position of the SubItem.
            // Because the columns can be reordered we have to use Columns[order[i]] instead of Columns[i] !
            ColumnHeader col;
            int i;
            for (i = 0; i < order.Length; i++)
            {
                col = this.Columns[order[i]];
                if (col.Index == SubItem)
                    break;
                subItemX += col.Width;
            }

            subItemRect = new Rectangle(subItemX, lviBounds.Top, this.Columns[order[i]].Width, lviBounds.Height);

            return subItemRect;
        }

        /// <summary>
        /// Add a control to the ListView
        /// </summary>
        /// <param name="c">Control to be added</param>
        /// <param name="col">Index of column</param>
        /// <param name="row">Index of row</param>
        public void AddEmbeddedControl(Control c, int col, int row)
        {
            AddEmbeddedControl(c, col, row, DockStyle.Fill);
        }

        /// <summary>
        /// Add a control to the ListView
        /// </summary>
        /// <param name="c">Control to be added</param>
        /// <param name="col">Index of column</param>
        /// <param name="row">Index of row</param>
        /// <param name="dock">Location and resize behavior of embedded control</param>
        public void AddEmbeddedControl(Control c, int col, int row, DockStyle dock)
        {
            if (c == null)
                throw new ArgumentNullException();
            if (col >= Columns.Count || row >= Items.Count)
                throw new ArgumentOutOfRangeException();

            EmbeddedControl ec;
            ec.Control = c;
            ec.Column = col;
            ec.Row = row;
            ec.Dock = dock;
            ec.Item = Items[row];

            _embeddedControls.Add(ec);

            // Add a Click event handler to select the ListView row when an embedded control is clicked
            c.Click += new EventHandler(_embeddedControl_Click);

            this.Controls.Add(c);
        }

        /// <summary>
        /// Remove a control from the ListView
        /// </summary>
        /// <param name="c">Control to be removed</param>
        public void RemoveEmbeddedControl(Control c)
        {
            if (c == null)
                throw new ArgumentNullException();

            for (int i=0; i < _embeddedControls.Count; i++)
            {
                EmbeddedControl ec = (EmbeddedControl)_embeddedControls[i];
                if (ec.Control == c)
                {
                    c.Click -= new EventHandler(_embeddedControl_Click);
                    this.Controls.Remove(c);
                    _embeddedControls.Remove(ec);
                    return;
                }
            }
            throw new Exception("Control not found!");
        }

        /// <summary>
        /// Retrieve the control embedded at a given location
        /// </summary>
        /// <param name="col">Index of Column</param>
        /// <param name="row">Index of Row</param>
        /// <returns>Control found at given location or null if none assigned.</returns>
        public Control GetEmbeddedControl(int col, int row)
        {
            foreach (EmbeddedControl ec in _embeddedControls)
                if (ec.Row == row && ec.Column == col)
                    return ec.Control;

            return null;
        }

        [DefaultValue(System.Windows.Forms.View.LargeIcon)]
        public new System.Windows.Forms.View View
        {
            get
            {
                return base.View;
            }
            set
            {
                // Embedded controls are rendered only when we're in Details mode
                foreach (EmbeddedControl ec in _embeddedControls)
                    ec.Control.Visible = (value == System.Windows.Forms.View.Details);

                base.View = value;
            }
        }

        protected override void WndProc(ref Message m)
        {
            ExtendedListView.ShowScrollBar(this.Handle, (int)0, false);

            switch (m.Msg)
            {
                case WM_PAINT:
                    if (View != System.Windows.Forms.View.Details)
                        break;

                    // Calculate the position of all embedded controls
                    foreach (EmbeddedControl ec in _embeddedControls)
                    {
                        Rectangle rc = this.GetSubItemBounds(ec.Item, ec.Column);

                        if ((this.HeaderStyle != ColumnHeaderStyle.None) &&
                            (rc.Top < this.Font.Height)) // Control overlaps ColumnHeader
                        {
                            ec.Control.Visible = false;
                            continue;
                        }
                        else
                        {
                            ec.Control.Visible = true;
                        }

                        switch (ec.Dock)
                        {
                            case DockStyle.Fill:
                                break;
                            case DockStyle.Top:
                                rc.Height = ec.Control.Height;
                                break;
                            case DockStyle.Left:
                                rc.Width = ec.Control.Width;
                                break;
                            case DockStyle.Bottom:
                                rc.Offset(0, rc.Height - ec.Control.Height);
                                rc.Height = ec.Control.Height;
                                break;
                            case DockStyle.Right:
                                rc.Offset(rc.Width - ec.Control.Width, 0);
                                rc.Width = ec.Control.Width;
                                break;
                            case DockStyle.None:
                                rc.Size = ec.Control.Size;
                                break;
                        }

                        // Set embedded control's bounds
                        ec.Control.Bounds = rc;
                    }
                    break;
            }
            base.WndProc(ref m);
        }

        /// <summary>
        /// When the control is created capture the messages for the header. 
        /// </summary>
        protected override void OnCreateControl()
        {
            //First actually create the control.
            base.OnCreateControl();

            //Now create the HeaderControl class to handle the customization of
            //the header messages.
            hdrCtrl = new HeaderControl(this);
        } //OnCreateControl()

        private void _embeddedControl_Click(object sender, EventArgs e)
        {
            // When a control is clicked the ListViewItem holding it is selected
            foreach (EmbeddedControl ec in _embeddedControls)
            {
                if (ec.Control == (Control)sender)
                {
                    this.SelectedItems.Clear();
                    ec.Item.Selected = true;
                }
            }
        }

        private void this_DrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
        {
            e.DrawDefault = true;
        }

        private void this_DrawItem(object sender, DrawListViewItemEventArgs e)
        {
            e.DrawDefault = false;



            // Something code
        }

        private void this_DrawSubItem(object sender, DrawListViewSubItemEventArgs e)
        {
            // Draw an item by only owner 
            e.DrawDefault = false;
            // Draw a background of the subitem using his current color
            e.DrawBackground();

            var itemIsSelected = (e.ItemState & ListViewItemStates.Selected) != 0;

            // If item is selected then ...
            if (itemIsSelected && e.ColumnIndex == 0)
            {
                int _x  = e.Bounds.X;
                int _y  = e.Bounds.Y;

                e.Graphics.FillRectangle(_highlightbrush, e.Bounds);
                e.Graphics.DrawString(e.SubItem.Text, e.Item.Font, new SolidBrush(Color.White), _x, _y);
            }

            // If the subitem is a subitem with an image then ...
            var subitem = e.SubItem as EXImageListViewSubItem;     // change
            if (subitem != null)
            {
                e.Graphics.DrawImage(this.SmallImageList.Images[e.ColumnIndex - 1], new Point(e.Bounds.X, e.Bounds.Y - 1));
            }

            // If the current item is not selected
            if (!itemIsSelected && e.ColumnIndex == 0)
            {
                int _x  = e.Bounds.X;
                int _y  = e.Bounds.Y;

                e.Graphics.DrawString(" " + e.SubItem.Text, e.SubItem.Font, new SolidBrush(Color.Black), _x, _y);

                // All operations are completed, so we can go out from the method
                return;
            }
        }
    }

    public class EXColumnHeader : ColumnHeader
    {

        public EXColumnHeader()
        {

        }

        public EXColumnHeader(string text)
        {
            this.Text = text;
        }

        public EXColumnHeader(string text, int width)
        {
            this.Text = text;
            this.Width = width;
        }

    }

    public class EXEditableColumnHeader : EXColumnHeader
    {

        private Control _control;

        public EXEditableColumnHeader()
        {

        }

        public EXEditableColumnHeader(string text)
        {
            this.Text = text;
        }

        public EXEditableColumnHeader(string text, int width)
        {
            this.Text = text;
            this.Width = width;
        }

        public EXEditableColumnHeader(string text, Control control)
        {
            this.Text = text;
            this.MyControl = control;
        }

        public EXEditableColumnHeader(string text, Control control, int width)
        {
            this.Text = text;
            this.MyControl = control;
            this.Width = width;
        }

        public Control MyControl
        {
            get { return _control; }
            set
            {
                _control = value;
                _control.Visible = false;
                _control.Tag = "not_init";
            }
        }

    }

    public class EXBoolColumnHeader : EXColumnHeader
    {

        private Image _trueimage;
        private Image _falseimage;
        private bool _editable;

        public EXBoolColumnHeader()
        {
            init();
        }

        public EXBoolColumnHeader(string text)
        {
            init();
            this.Text = text;
        }

        public EXBoolColumnHeader(string text, int width)
        {
            init();
            this.Text = text;
            this.Width = width;
        }

        public EXBoolColumnHeader(string text, Image trueimage, Image falseimage)
        {
            init();
            this.Text = text;
            _trueimage = trueimage;
            _falseimage = falseimage;
        }

        public EXBoolColumnHeader(string text, Image trueimage, Image falseimage, int width)
        {
            init();
            this.Text = text;
            _trueimage = trueimage;
            _falseimage = falseimage;
            this.Width = width;
        }

        private void init()
        {
            _editable = false;
        }

        public Image TrueImage
        {
            get { return _trueimage; }
            set { _trueimage = value; }
        }

        public Image FalseImage
        {
            get { return _falseimage; }
            set { _falseimage = value; }
        }

        public bool Editable
        {
            get { return _editable; }
            set { _editable = value; }
        }
    }

    public abstract class EXListViewSubItemAB : ListViewItem.ListViewSubItem
    {

        private string _value = "";

        public EXListViewSubItemAB()
        {

        }

        public EXListViewSubItemAB(string text)
        {
            this.Text = text;
        }

        public string MyValue
        {
            get { return _value; }
            set { _value = value; }
        }

        //return the new x coordinate
        public abstract int DoDraw(DrawListViewSubItemEventArgs e, int x, EXColumnHeader ch);

    }

    public class EXListViewSubItem : EXListViewSubItemAB
    {

        public EXListViewSubItem()
        {

        }

        public EXListViewSubItem(string text)
        {
            this.Text = text;
        }

        public override int DoDraw(DrawListViewSubItemEventArgs e, int x, EXColumnHeader ch)
        {
            return x;
        }

    }

    public class EXControlListViewSubItem : EXListViewSubItemAB
    {

        private Control _control;

        public EXControlListViewSubItem()
        {

        }

        public Control MyControl
        {
            get { return _control; }
            set { _control = value; }
        }

        public override int DoDraw(DrawListViewSubItemEventArgs e, int x, EXColumnHeader ch)
        {
            return x;
        }

    }

    public class EXImageListViewSubItem : EXListViewSubItemAB
    {

        private Image _image;

        public EXImageListViewSubItem()
        {

        }

        public EXImageListViewSubItem(string text)
        {
            this.Text = text;
        }

        public EXImageListViewSubItem(Image image)
        {
            _image = image;
        }

        public EXImageListViewSubItem(Image image, string value)
        {
            _image = image;
            this.MyValue = value;
        }

        public EXImageListViewSubItem(string text, Image image, string value)
        {
            this.Text = text;
            _image = image;
            this.MyValue = value;
        }

        public Image MyImage
        {
            get { return _image; }
            set { _image = value; }
        }

        public override int DoDraw(DrawListViewSubItemEventArgs e, int x, EXColumnHeader ch)
        {
            if (this.MyImage != null)
            {
                Image img = this.MyImage;
                int imgy = e.Bounds.Y + ((int)(e.Bounds.Height / 2)) - ((int)(img.Height / 2));
                e.Graphics.DrawImage(img, x, imgy, img.Width, img.Height);
                x += img.Width + 2;
            }
            return x;
        }

    }

    public class EXMultipleImagesListViewSubItem : EXListViewSubItemAB
    {

        private ArrayList _images;

        public EXMultipleImagesListViewSubItem()
        {

        }

        public EXMultipleImagesListViewSubItem(string text)
        {
            this.Text = text;
        }

        public EXMultipleImagesListViewSubItem(ArrayList images)
        {
            _images = images;
        }

        public EXMultipleImagesListViewSubItem(ArrayList images, string value)
        {
            _images = images;
            this.MyValue = value;
        }

        public EXMultipleImagesListViewSubItem(string text, ArrayList images, string value)
        {
            this.Text = text;
            _images = images;
            this.MyValue = value;
        }

        public ArrayList MyImages
        {
            get { return _images; }
            set { _images = value; }
        }

        public override int DoDraw(DrawListViewSubItemEventArgs e, int x, EXColumnHeader ch)
        {
            if (this.MyImages != null && this.MyImages.Count > 0)
            {
                for (int i = 0; i < this.MyImages.Count; i++)
                {
                    Image img = (Image)this.MyImages[i];
                    int imgy = e.Bounds.Y + ((int)(e.Bounds.Height / 2)) - ((int)(img.Height / 2));
                    e.Graphics.DrawImage(img, x, imgy, img.Width, img.Height);
                    x += img.Width + 2;
                }
            }
            return x;
        }

    }

    public class EXBoolListViewSubItem : EXListViewSubItemAB
    {

        private bool _value;

        public EXBoolListViewSubItem()
        {

        }

        public EXBoolListViewSubItem(bool val)
        {
            _value = val;
            this.MyValue = val.ToString();
        }

        public bool BoolValue
        {
            get { return _value; }
            set
            {
                _value = value;
                this.MyValue = value.ToString();
            }
        }

        public override int DoDraw(DrawListViewSubItemEventArgs e, int x, EXColumnHeader ch)
        {
            EXBoolColumnHeader boolcol = (EXBoolColumnHeader)ch;
            Image boolimg;
            if (this.BoolValue == true)
            {
                boolimg = boolcol.TrueImage;
            }
            else
            {
                boolimg = boolcol.FalseImage;
            }
            int imgy = e.Bounds.Y + ((int)(e.Bounds.Height / 2)) - ((int)(boolimg.Height / 2));
            e.Graphics.DrawImage(boolimg, x, imgy, boolimg.Width, boolimg.Height);
            x += boolimg.Width + 2;
            return x;
        }

    }

    public class EXListViewItem : ListViewItem
    {

        private string _value;

        public EXListViewItem()
        {

        }

        public EXListViewItem(string text)
        {
            this.Text = text;
        }

        public string MyValue
        {
            get { return _value; }
            set { _value = value; }
        }

    }

    public class EXImageListViewItem : EXListViewItem
    {

        private Image _image;

        public EXImageListViewItem()
        {

        }

        public EXImageListViewItem(string text)
        {
            this.Text = text;
        }

        public EXImageListViewItem(Image image)
        {
            _image = image;
        }

        public EXImageListViewItem(string text, Image image)
        {
            _image = image;
            this.Text = text;
        }

        public EXImageListViewItem(string text, Image image, string value)
        {
            this.Text = text;
            _image = image;
            this.MyValue = value;
        }

        public Image MyImage
        {
            get { return _image; }
            set { _image = value; }
        }

    }

    public class EXMultipleImagesListViewItem : EXListViewItem
    {

        private ArrayList _images;

        public EXMultipleImagesListViewItem()
        {

        }

        public EXMultipleImagesListViewItem(string text)
        {
            this.Text = text;
        }

        public EXMultipleImagesListViewItem(ArrayList images)
        {
            _images = images;
        }

        public EXMultipleImagesListViewItem(string text, ArrayList images)
        {
            this.Text = text;
            _images = images;
        }

        public EXMultipleImagesListViewItem(string text, ArrayList images, string value)
        {
            this.Text = text;
            _images = images;
            this.MyValue = value;
        }

        public ArrayList MyImages
        {
            get { return _images; }
            set { _images = value; }
        }
    }


    /// <summary>
    /// Class used to capture window messages for the header of the list view control.
    /// </summary>
    public class HeaderControl : NativeWindow
    {
        private ExtendedListView parentListView = null;

        [DllImport("User32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        public HeaderControl(ExtendedListView m)
        {
            parentListView = m;
            //Get the header control handle
            IntPtr header = SendMessage(m.Handle,
                (0x1000 + 31), IntPtr.Zero, IntPtr.Zero);
            this.AssignHandle(header);
        } //constructor HeaderControl()

        protected override void WndProc(ref Message message)
        {
            const int WM_LBUTTONDOWN = 0x0201;
            const int WM_LBUTTONDBLCLK = 0x0203;
            const int WM_SETCURSOR = 0x0020;
            //const int LVM_SETEXTENDEDLISTVIEWSTYLE = 0x1000 + 54;

            bool callBase = true;
            switch (message.Msg)
            {
                case WM_LBUTTONDBLCLK:
                case WM_LBUTTONDOWN:
                //case LVM_SETEXTENDEDLISTVIEWSTYLE:
                case WM_SETCURSOR:
                    if (parentListView.LockColumnSize)
                    {
                        //Don't change cursor to sizing cursor.  Also ignore
                        //double click, which sizes the column to fit the data.
                        message.Result = (IntPtr)1;	//Return TRUE from message handler
                        callBase = false;		//Don't call the base class.
                    } //if
                    break;
            } //switch

            if (callBase)
            {
                // pass messages on to the base control for processing
                base.WndProc(ref message);
            } //if
        } //WndProc()
    } //class HeaderControl
}
