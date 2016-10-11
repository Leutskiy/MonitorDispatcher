using System;
using System.Windows.Forms;
using System.Drawing;
using System.Collections;
using System.Runtime.InteropServices;
using System.Drawing.Drawing2D;

namespace Sender.EXControls {

    using Sender.View;
    using System.ComponentModel;


    public class EXListView : ListView {

        
        /// <summary>
        /// Class used to capture window messages for the header of the list view
        /// control.  
        /// </summary>
        private class HeaderControl : NativeWindow
        {
            private EXListView parentListView = null;

            [DllImport("User32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

            public HeaderControl(EXListView m)
            {
                parentListView = m;
                //Get the header control handle
                IntPtr header = SendMessage(m.Handle,
                    (0x1000 + 31), IntPtr.Zero, IntPtr.Zero);
                this.AssignHandle(header);
            } //constructor HeaderControl()

            protected override void WndProc(ref Message message)
            {
                const int WM_LBUTTONDBLCLK = 0x0203;
                const int WM_SETCURSOR = 0x0020;
                const int LVM_SETEXTENDEDLISTVIEWSTYLE = 0x1000 + 54;
                bool callBase = true;

                switch (message.Msg)
                {
                    case WM_LBUTTONDBLCLK:
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
        

        /// <summary>
        /// Notify message header structure.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct NMHDR
        {
            public IntPtr hwndFrom;
            public int idFrom;
            public int code;
        }

        private ListViewItem.ListViewSubItem _clickedsubitem; //clicked ListViewSubItem
        private ListViewItem _clickeditem; //clicked ListViewItem
        private int _col; //index of doubleclicked ListViewSubItem
        private TextBox txtbx; //the default edit control
        private int _sortcol; //index of clicked ColumnHeader
        private Brush _sortcolbrush; //color of items in sorted column
        private Brush _highlightbrush; //color of highlighted items
        private int _cpadding; //padding of the embedded controls
        private Rectangle _rect;

        private HeaderControl hdrCtrl = null;
        private bool locked = true;

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
            
        private const UInt32 LVM_FIRST = 0x1000;
        private const UInt32 LVM_SCROLL = (LVM_FIRST + 20);
        private const int WM_HSCROLL = 0x114;
        private const int WM_VSCROLL = 0x115;
        private const int WM_MOUSEWHEEL = 0x020A;
        private const int WM_PAINT = 0x000F;
        private const int WM_NCMOUSEMOVE = 0x00A0;
        private const int WM_MOUSEMOVE = 0x0200;
        private const UInt32 LVM_FIRST_L = 0x1000;
        private const UInt32 LVM_GETHEADER = (LVM_FIRST_L + 31);

        //private const int WM_NOTIFY = 0x004E;
        //private const int HDN_FIRST = (0 - 300);
        //private const int HDN_BEGINTRACKA = (HDN_FIRST - 6);
        //private const int HDN_BEGINTRACKW = (HDN_FIRST - 26);
        //private const int WM_LBUTTONDBLCLK = 0x0203;
        //private const int WM_SETCURSOR = 0x0020;
            
        private struct EmbeddedControl {
            public Control MyControl;
            public EXControlListViewSubItem MySubItem;
        }
            
        private ArrayList _controls;

        [Serializable, StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("User32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool SendMessage(IntPtr hWnd, UInt32 m, int wParam, int lParam);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(HandleRef hwnd, out RECT lpRect);


        
        
        /// <summary>
        /// Capture messages for the list view control.
        /// </summary>
        /// <param name="message"></param>
        protected override void WndProc(ref Message message)
        {
            if (message.Msg == WM_PAINT)
            {
                foreach (EmbeddedControl c in _controls)
                {
                    Rectangle r = c.MySubItem.Bounds;
                    if (r.Y > 0 && r.Y < this.ClientRectangle.Height)
                    {
                        c.MyControl.Visible = true;
                        c.MyControl.Bounds = new Rectangle(r.X + _cpadding, r.Y + _cpadding, r.Width - (2 * _cpadding), r.Height - (2 * _cpadding));
                    }
                    else
                    {
                        c.MyControl.Visible = false;
                    }
                }
            }

            const int WM_NOTIFY = 0x004E;
            const int HDN_FIRST = (0 - 300);
            const int HDN_BEGINTRACKA = (HDN_FIRST - 6);
            const int HDN_BEGINTRACKW = (HDN_FIRST - 26);
         
            bool callBase = true;

            switch (message.Msg)
            {
                case WM_NOTIFY:
                    {
                        NMHDR nmhdr = (NMHDR)message.GetLParam(typeof(NMHDR));
                        switch (nmhdr.code)
                        {
                            case HDN_BEGINTRACKA:  //Process both ANSI and
                            case HDN_BEGINTRACKW:  //UNICODE versions of the message.
                                if (locked)
                                {
                                    
                                    //Discard the begin tracking to prevent dragging of the 
                                    //column headers.
                                    message.Result = (IntPtr)1;
                                    callBase = false;
                                } //if
                                break;
                            //case LVM_GETHEADER:
                            //    {
                            //        MessageBox.Show("Text");
                            //        this.Cursor = Cursors.Default;
                            //        break;
                            //    }
                            
                        } //switch
                        break;
                    }
            } //switch

            if (callBase)
            {
                // pass messages on to the base control for processing
                base.WndProc(ref message);
            } //if
        } //WndProc()


        private RECT rc_header;
        private Rectangle header_rect;
        private IntPtr hwnd;

        private void ScrollMe(int x, int y) {
            SendMessage((IntPtr) this.Handle, LVM_SCROLL, x, y);
        }
        
        public EXListView() {
            _cpadding = 4;
            _controls = new ArrayList();
            _sortcol = -1;
            _sortcolbrush = SystemBrushes.ControlLight;
            _highlightbrush = SystemBrushes.Highlight;
            _rect = Rectangle.Empty;

            this.OwnerDraw = true;
            //this.FullRowSelect = false;
            //this.HideSelection = true;
            //this.Dock = DockStyle.Fill;
            this.View = System.Windows.Forms.View.Details;


            this.MouseMove += new MouseEventHandler(this_MouseMove);
            //this.MouseDown += new MouseEventHandler(this_MouseDown);
            //this.MouseDoubleClick += new MouseEventHandler(this_MouseDoubleClick);
            this.DrawItem += new DrawListViewItemEventHandler(this_DrawItem);
            this.DrawColumnHeader += new DrawListViewColumnHeaderEventHandler(this_DrawColumnHeader);
            this.DrawSubItem += new DrawListViewSubItemEventHandler(this_DrawSubItem);
            

            //this.ColumnWidthChanging += new ColumnWidthChangingEventHandler(this_ColumnWidthChanging);
            
            this.MouseClick += new MouseEventHandler(this_MouseClick);
            //this.ColumnClick += new ColumnClickEventHandler(this_ColumnClick);


            //txtbx = new TextBox();
            //txtbx.Visible = false;
            //this.Controls.Add(txtbx);
            //txtbx.Leave += new EventHandler(c_Leave);
            //txtbx.KeyPress += new KeyPressEventHandler(txtbx_KeyPress);
        }

        private int GetColumnIndex(Point p)
        {
            _rect = Rectangle.Empty;
            for (int i = 0; i < this.Columns.Count; i++)
            {
                _rect = new Rectangle(_rect.X + _rect.Width, 0, this.Columns[i].Width, this.Height);
                if (_rect.Contains(p) )
                    return i;
                else
                {
                    //GetColumnIndexHeader(p);
                }
            }
            return -1;
        }

        private void this_MouseClick(object sender, MouseEventArgs e)
        {
            //MessageBox.Show("Text");
        }

        private void this_MouseMove(object sender, MouseEventArgs e)
        {
            //if (!header_rect.Contains(e.Location))
            //{
            //    //MessageBox.Show("Text");
            //    ///this.Cursor = Cursors.AppStarting;
            //}

            /*
            if (_rect.Contains(e.Location))
                return;

            int columnIndex = GetColumnIndex(e.Location);


            if (columnIndex == 1 || columnIndex == 2)
                this.Cursor = Cursors.Hand;
            else
                this.Cursor = Cursors.Default;
             * */

            //MessageBox.Show(header_rect.Height.ToString() + header_rect.Width.ToString());

            



        }

        private void this_DrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
        {
            e.DrawDefault = true;
        }

        private void this_DrawItem(object sender, DrawListViewItemEventArgs e)
        {
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
            var subitem = e.SubItem as EXImageListViewSubItem;
            if (subitem != null)
            {
                e.Graphics.DrawImage(this.SmallImageList.Images[e.ColumnIndex - 1], new Point(e.Bounds.X, e.Bounds.Y -1));
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

        //public void this_ColumnWidthChanging(object sender, ColumnWidthChangingEventArgs e)
        //{
        //    e.Cancel = true;
        //    e.NewWidth = this.Columns[e.ColumnIndex].Width;
        //}

        //public void AddControlToSubItem(Control control, EXControlListViewSubItem subitem)
        //{
        //    this.Controls.Add(control);
        //    subitem.MyControl = control;
        //    EmbeddedControl ec;
        //    ec.MyControl = control;
        //    ec.MySubItem = subitem;
        //    this._controls.Add(ec);
        //}
        
        //public void RemoveControlFromSubItem(EXControlListViewSubItem subitem) {
        //    Control c = subitem.MyControl;
        //    for (int i = 0; i < this._controls.Count; i++) {
        //        if (((EmbeddedControl) this._controls[i]).MySubItem == subitem) {
        //            this._controls.RemoveAt(i);
        //            subitem.MyControl = null;
        //            this.Controls.Remove(c);
        //            c.Dispose();
        //            return;
        //        }
        //    }
        //}
        
        public int ControlPadding {
            get {return _cpadding;}
            set {_cpadding = value;}
        }
        
        public Brush MySortBrush {
            get {return _sortcolbrush;}
            set {_sortcolbrush = value;}
        }
        
        public Brush MyHighlightBrush {
            get {return _highlightbrush;}
            set {_highlightbrush = value;}
        }
        
        //private void txtbx_KeyPress(object sender, KeyPressEventArgs e) {
        //    if (e.KeyChar == (char) Keys.Return) {
        //        _clickedsubitem.Text = txtbx.Text;
        //        txtbx.Visible = false;
        //        _clickeditem.Tag = null;
        //    }
        //}
        
        //private void c_Leave(object sender, EventArgs e) {
        //    Control c = (Control) sender;
        //    _clickedsubitem.Text = c.Text;
        //    c.Visible = false;
        //    _clickeditem.Tag = null;
        //}
        
        //private void this_MouseDown(object sender, MouseEventArgs e) {
        //    ListViewHitTestInfo lstvinfo = this.HitTest(e.X, e.Y);
        //    ListViewItem.ListViewSubItem subitem = lstvinfo.SubItem;
        //    if (subitem == null) return;
        //    int subx = subitem.Bounds.Left;
        //    if (subx < 0) {
        //        this.ScrollMe(subx, 0);
        //    }
        //}
        
        //private void this_MouseDoubleClick(object sender, MouseEventArgs e) {

        //    EXListViewItem lstvItem = this.GetItemAt(e.X, e.Y) as EXListViewItem;

        //    //MessageBox.Show("width and height = " + e.);
        //    //MessageBox.Show(lstvItem.Text + " x = "+ e.X + " Y = " + e.Y);

        //    if (lstvItem == null) return;
        //    _clickeditem = lstvItem;
        //    int x = lstvItem.Bounds.Left;
        //    int i;
        //    for (i = 0; i < this.Columns.Count; i++) {
        //        x = x + this.Columns[i].Width;
        //        if (x > e.X) {
        //            x = x - this.Columns[i].Width;
        //            _clickedsubitem = lstvItem.SubItems[i];
                    
        //            _col = i;
        //            break;
        //        }
        //    }
        //    
        //    if (!(this.Columns[i] is EXColumnHeader)) return;
        //    EXColumnHeader col = (EXColumnHeader) this.Columns[i];
        //    if (col.GetType() == typeof(EXEditableColumnHeader)) {
        //        EXEditableColumnHeader editcol = (EXEditableColumnHeader) col;
        //        if (editcol.MyControl != null) {
        //            Control c = editcol.MyControl;
        //            if (c.Tag != null) {
        //                this.Controls.Add(c);
        //                c.Tag = null;
        //                if (c is ComboBox) {
        //                    ((ComboBox) c).SelectedValueChanged += new EventHandler(cmbx_SelectedValueChanged);
        //                }
        //                c.Leave += new EventHandler(c_Leave);
        //            }
        //            c.Location = new Point(x, this.GetItemRect(this.Items.IndexOf(lstvItem)).Y);
        //            c.Width = this.Columns[i].Width;
        //            if (c.Width > this.Width) c.Width = this.ClientRectangle.Width;
        //            c.Text = _clickedsubitem.Text;
        //            c.Visible = true;
        //            c.BringToFront();
        //            c.Focus();
        //        } else {
        //            txtbx.Location = new Point(x, this.GetItemRect(this.Items.IndexOf(lstvItem)).Y);
        //            txtbx.Width = this.Columns[i].Width;
        //            if (txtbx.Width > this.Width) txtbx.Width = this.ClientRectangle.Width;
        //            txtbx.Text = _clickedsubitem.Text;
        //            txtbx.Visible = true;
        //            txtbx.BringToFront();
        //            txtbx.Focus();
        //        }
        //    } else if (col.GetType() == typeof(EXBoolColumnHeader)) {
        //        EXBoolColumnHeader boolcol = (EXBoolColumnHeader) col;
        //        if (boolcol.Editable) {
        //            EXBoolListViewSubItem boolsubitem = (EXBoolListViewSubItem) _clickedsubitem;
        //            if (boolsubitem.BoolValue == true) {
        //                boolsubitem.BoolValue = false;
        //            } else {
        //                boolsubitem.BoolValue = true;
        //            }
        //            this.Invalidate(boolsubitem.Bounds);
        //        }
        //    }
        //}
        
        //private void cmbx_SelectedValueChanged(object sender, EventArgs e) {
        //    if (((Control) sender).Visible == false || _clickedsubitem == null) return;
        //    if (sender.GetType() == typeof(EXComboBox)) {
        //        EXComboBox excmbx = (EXComboBox) sender;
        //        object item = excmbx.SelectedItem;
        //        //Is this an combobox item with one image?
        //        if (item.GetType() == typeof(EXComboBox.EXImageItem)) {
        //            EXComboBox.EXImageItem imgitem = (EXComboBox.EXImageItem) item;
        //            //Is the first column clicked -- in that case it's a ListViewItem
        //            if (_col == 0) {
        //                if (_clickeditem.GetType() == typeof(EXImageListViewItem)) {
        //                    ((EXImageListViewItem) _clickeditem).MyImage = imgitem.MyImage;
        //                } else if (_clickeditem.GetType() == typeof(EXMultipleImagesListViewItem)) {
        //                    EXMultipleImagesListViewItem imglstvitem = (EXMultipleImagesListViewItem) _clickeditem;
        //                    imglstvitem.MyImages.Clear();
        //                    imglstvitem.MyImages.AddRange(new object[] {imgitem.MyImage});
        //                }
        //            //another column than the first one is clicked, so we have a ListViewSubItem
        //            } else {
        //                if (_clickedsubitem.GetType() == typeof(EXImageListViewSubItem)) {
        //                    EXImageListViewSubItem imgsub = (EXImageListViewSubItem) _clickedsubitem;
        //                    imgsub.MyImage = imgitem.MyImage;
        //                } else if (_clickedsubitem.GetType() == typeof(EXMultipleImagesListViewSubItem)) {
        //                    EXMultipleImagesListViewSubItem imgsub = (EXMultipleImagesListViewSubItem) _clickedsubitem;
        //                    imgsub.MyImages.Clear();
        //                    imgsub.MyImages.Add(imgitem.MyImage);
        //                    imgsub.MyValue = imgitem.MyValue;
        //                }
        //            }
        //            //or is this a combobox item with multiple images?
        //        } else if (item.GetType() == typeof(EXComboBox.EXMultipleImagesItem)) {
        //            EXComboBox.EXMultipleImagesItem imgitem = (EXComboBox.EXMultipleImagesItem) item;
        //            if (_col == 0) {
        //                if (_clickeditem.GetType() == typeof(EXImageListViewItem)) {
        //                    ((EXImageListViewItem) _clickeditem).MyImage = (Image) imgitem.MyImages[0];
        //                } else if (_clickeditem.GetType() == typeof(EXMultipleImagesListViewItem)) {
        //                    EXMultipleImagesListViewItem imglstvitem = (EXMultipleImagesListViewItem) _clickeditem;
        //                    imglstvitem.MyImages.Clear();
        //                    imglstvitem.MyImages.AddRange(imgitem.MyImages);
        //                }
        //            } else {
        //                if (_clickedsubitem.GetType() == typeof(EXImageListViewSubItem)) {
        //                    EXImageListViewSubItem imgsub = (EXImageListViewSubItem) _clickedsubitem;
        //                    if (imgitem.MyImages != null) {
        //                        imgsub.MyImage = (Image) imgitem.MyImages[0];
        //                    }
        //                } else if (_clickedsubitem.GetType() == typeof(EXMultipleImagesListViewSubItem)) {
        //                    EXMultipleImagesListViewSubItem imgsub = (EXMultipleImagesListViewSubItem) _clickedsubitem;
        //                    imgsub.MyImages.Clear();
        //                    imgsub.MyImages.AddRange(imgitem.MyImages);
        //                    imgsub.MyValue = imgitem.MyValue;
        //                }
        //            }
        //        }
        //    }
        //    ComboBox c = (ComboBox) sender;
        //    _clickedsubitem.Text = c.Text;
        //    c.Visible = false;
        //    _clickeditem.Tag = null;
        //}
        
        
        
        
        
        //private void this_ColumnClick(object sender, ColumnClickEventArgs e) {
        //    if (this.Items.Count == 0) return;
        //    for (int i = 0; i < this.Columns.Count; i++) {
        //        this.Columns[i].ImageKey = null;
        //    }
        //    for (int i = 0; i < this.Items.Count; i++) {
        //        this.Items[i].Tag = null;
        //    }
        //    if (e.Column != _sortcol) {
        //        _sortcol = e.Column;
        //        this.Sorting = SortOrder.Ascending;
        //        this.Columns[e.Column].ImageKey = "up";
        //    } else {
        //        if (this.Sorting == SortOrder.Ascending) {
        //            this.Sorting = SortOrder.Descending;
        //            this.Columns[e.Column].ImageKey = "down";
        //        } else {
        //            this.Sorting = SortOrder.Ascending;
        //            this.Columns[e.Column].ImageKey = "up";
        //        }
        //    }
        //    if (_sortcol == 0) {
        //        //ListViewItem
        //        if (this.Items[0].GetType() == typeof(EXListViewItem)) {
        //            //sorting on text
        //            this.ListViewItemSorter = new ListViewItemComparerText(e.Column, this.Sorting);
        //        } else {
        //            //sorting on value
        //            this.ListViewItemSorter = new ListViewItemComparerValue(e.Column, this.Sorting);
        //        }
        //    } else {
        //        //ListViewSubItem
        //        if (this.Items[0].SubItems[_sortcol].GetType() == typeof(EXListViewSubItemAB)) {
        //            //sorting on text
        //            this.ListViewItemSorter = new ListViewSubItemComparerText(e.Column, this.Sorting);
        //        } else {
        //            //sorting on value
        //            this.ListViewItemSorter = new ListViewSubItemComparerValue(e.Column, this.Sorting);
        //        }
        //    }
        //}

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

        /// <summary>
        /// Capture CTRL+ to prevent resize of all columns.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyValue == 107 && e.Modifiers == Keys.Control && locked)
            {
                e.Handled = true;
            }
            else
            {
                base.OnKeyDown(e);
            } //if
        } //OnKeyDown()
        
        class ListViewSubItemComparerText : System.Collections.IComparer {
            
            private int _col;
            private SortOrder _order;

            public ListViewSubItemComparerText() {
                _col = 0;
                _order = SortOrder.Ascending;
            }

            public ListViewSubItemComparerText(int col, SortOrder order) {
                _col = col;
                _order = order;
            }
            
            public int Compare(object x, object y) {
                int returnVal = -1;
                
                string xstr = ((ListViewItem) x).SubItems[_col].Text;
                string ystr = ((ListViewItem) y).SubItems[_col].Text;
                
                decimal dec_x;
                decimal dec_y;
                DateTime dat_x;
                DateTime dat_y;
                
                if (Decimal.TryParse(xstr, out dec_x) && Decimal.TryParse(ystr, out dec_y)) {
                    returnVal = Decimal.Compare(dec_x, dec_y);
                } else if (DateTime.TryParse(xstr, out dat_x) && DateTime.TryParse(ystr, out dat_y)) {
                    returnVal = DateTime.Compare(dat_x, dat_y);
                } else {
                    returnVal = String.Compare(xstr, ystr);
                }
                if (_order == SortOrder.Descending) returnVal *= -1;
                return returnVal;
            }
        
        }
	
	    class ListViewSubItemComparerValue : System.Collections.IComparer {
            
            private int _col;
            private SortOrder _order;

            public ListViewSubItemComparerValue() {
                _col = 0;
                _order = SortOrder.Ascending;
            }

            public ListViewSubItemComparerValue(int col, SortOrder order) {
                _col = col;
                _order = order;
            }
            
            public int Compare(object x, object y) {
                int returnVal = -1;
                
                string xstr = ((EXListViewSubItemAB) ((ListViewItem) x).SubItems[_col]).MyValue;
                string ystr = ((EXListViewSubItemAB) ((ListViewItem) y).SubItems[_col]).MyValue;
                
                decimal dec_x;
                decimal dec_y;
                DateTime dat_x;
                DateTime dat_y;
                
                if (Decimal.TryParse(xstr, out dec_x) && Decimal.TryParse(ystr, out dec_y)) {
                    returnVal = Decimal.Compare(dec_x, dec_y);
                } else if (DateTime.TryParse(xstr, out dat_x) && DateTime.TryParse(ystr, out dat_y)) {
                    returnVal = DateTime.Compare(dat_x, dat_y);
                } else {
                    returnVal = String.Compare(xstr, ystr);
                }
                if (_order == SortOrder.Descending) returnVal *= -1;
                return returnVal;
            }
        
        }
	
	    class ListViewItemComparerText : System.Collections.IComparer {
            
            private int _col;
            private SortOrder _order;

            public ListViewItemComparerText() {
                _col = 0;
                _order = SortOrder.Ascending;
            }

            public ListViewItemComparerText(int col, SortOrder order) {
                _col = col;
                _order = order;
            }
            
            public int Compare(object x, object y) {
                int returnVal = -1;
                
                string xstr = ((ListViewItem) x).Text;
                string ystr = ((ListViewItem) y).Text;
                
                decimal dec_x;
                decimal dec_y;
                DateTime dat_x;
                DateTime dat_y;
                
                if (Decimal.TryParse(xstr, out dec_x) && Decimal.TryParse(ystr, out dec_y)) {
                    returnVal = Decimal.Compare(dec_x, dec_y);
                } else if (DateTime.TryParse(xstr, out dat_x) && DateTime.TryParse(ystr, out dat_y)) {
                    returnVal = DateTime.Compare(dat_x, dat_y);
                } else {
                    returnVal = String.Compare(xstr, ystr);
                }
                if (_order == SortOrder.Descending) returnVal *= -1;
                return returnVal;
            }
        
        }
	
	    class ListViewItemComparerValue : System.Collections.IComparer {
            
            private int _col;
            private SortOrder _order;

            public ListViewItemComparerValue() {
                _col = 0;
                _order = SortOrder.Ascending;
            }

            public ListViewItemComparerValue(int col, SortOrder order) {
                _col = col;
                _order = order;
            }
            
            public int Compare(object x, object y) {
                int returnVal = -1;
                
                string xstr = ((EXListViewItem) x).MyValue;
                string ystr = ((EXListViewItem) y).MyValue;
                
                decimal dec_x;
                decimal dec_y;
                DateTime dat_x;
                DateTime dat_y;
                
                if (Decimal.TryParse(xstr, out dec_x) && Decimal.TryParse(ystr, out dec_y)) {
                    returnVal = Decimal.Compare(dec_x, dec_y);
                } else if (DateTime.TryParse(xstr, out dat_x) && DateTime.TryParse(ystr, out dat_y)) {
                    returnVal = DateTime.Compare(dat_x, dat_y);
                } else {
                    returnVal = String.Compare(xstr, ystr);
                }
                if (_order == SortOrder.Descending) returnVal *= -1;
                return returnVal;
            }
        
        }
        
    }
    
    public class EXColumnHeader : ColumnHeader {
        
        public EXColumnHeader() {
            
        }
        
        public EXColumnHeader(string text) {
            this.Text = text;
        }
        
        public EXColumnHeader(string text, int width) {
            this.Text = text;
            this.Width = width;
        }
        
    }
    
    public class EXEditableColumnHeader : EXColumnHeader {
        
        private Control _control;
        
        public EXEditableColumnHeader() {
            
        }
        
        public EXEditableColumnHeader(string text) {
            this.Text = text;
        }
        
        public EXEditableColumnHeader(string text, int width) {
            this.Text = text;
            this.Width = width;
        }
        
        public EXEditableColumnHeader(string text, Control control) {
            this.Text = text;
            this.MyControl = control;
        }
        
        public EXEditableColumnHeader(string text, Control control, int width) {
            this.Text = text;
            this.MyControl = control;
            this.Width = width;
        }
        
        public Control MyControl {
            get {return _control;}
            set {
                _control = value;
                _control.Visible = false;
                _control.Tag = "not_init";
            }
        }
        
    }
    
    public class EXBoolColumnHeader : EXColumnHeader {
        
        private Image _trueimage;
        private Image _falseimage;
        private bool _editable;
            
        public EXBoolColumnHeader() {
            init();
        }
        
        public EXBoolColumnHeader(string text) {
            init();
            this.Text = text;
        }
        
        public EXBoolColumnHeader(string text, int width) {
            init();
            this.Text = text;
            this.Width = width;
        }
        
        public EXBoolColumnHeader(string text, Image trueimage, Image falseimage) {
            init();
            this.Text = text;
            _trueimage = trueimage;
            _falseimage = falseimage;
        }
        
        public EXBoolColumnHeader(string text, Image trueimage, Image falseimage, int width) {
            init();
            this.Text = text;
            _trueimage = trueimage;
            _falseimage = falseimage;
            this.Width = width;
        }
        
        private void init() {
            _editable = false;
        }
        
        public Image TrueImage {
            get {return _trueimage;}
            set {_trueimage = value;}
        }
        
        public Image FalseImage {
            get {return _falseimage;}
            set {_falseimage = value;}
        }
        
        public bool Editable {
            get {return _editable;}
            set {_editable = value;}
        }
        
    }
    
    public abstract class EXListViewSubItemAB : ListViewItem.ListViewSubItem {
        
        private string _value = "";
        
        public EXListViewSubItemAB() {
            
        }
        
        public EXListViewSubItemAB(string text) {
            this.Text = text;
        }
        
        public string MyValue {
            get {return _value;}
            set {_value = value;}
        }
        
        //return the new x coordinate
        public abstract int DoDraw(DrawListViewSubItemEventArgs e, int x, EXControls.EXColumnHeader ch);

    }
    
    public class EXListViewSubItem : EXListViewSubItemAB {
        
        public EXListViewSubItem() {
            
        }
        
        public EXListViewSubItem(string text) {
            this.Text = text;
        }
        
        public override int DoDraw(DrawListViewSubItemEventArgs e, int x, EXControls.EXColumnHeader ch) {
            return x;
        }

    }
    
    public class EXControlListViewSubItem : EXListViewSubItemAB {
        
        private Control _control;
            
        public EXControlListViewSubItem() {
            
        }
        
        public Control MyControl {
            get {return _control;}
            set {_control = value;}
        }
        
        public override int DoDraw(DrawListViewSubItemEventArgs e, int x, EXColumnHeader ch) {
            return x;
        }
        
    }
    
    public class EXImageListViewSubItem : EXListViewSubItemAB {
        
        private Image _image;
            
        public EXImageListViewSubItem() {
            
        }
        
        public EXImageListViewSubItem(string text) {
            this.Text = text;
        }
            
        public EXImageListViewSubItem(Image image) {
            _image = image;
        }
	
        public EXImageListViewSubItem(Image image, string value) {
            _image = image;
            this.MyValue = value;
        }
	
        public EXImageListViewSubItem(string text, Image image, string value) {
            this.Text = text;
            _image = image;
            this.MyValue = value;
        }
        
        public Image MyImage {
            get {return _image;}
            set {_image = value;}
        }
        
        public override int DoDraw(DrawListViewSubItemEventArgs e, int x, EXControls.EXColumnHeader ch) {
            if (this.MyImage != null) {
                Image img = this.MyImage;
                int imgy = e.Bounds.Y + ((int) (e.Bounds.Height / 2)) - ((int) (img.Height / 2));
                e.Graphics.DrawImage(img, x, imgy, img.Width, img.Height);
                x += img.Width + 2;
            }
            return x;
        }
        
    }
    
    public class EXMultipleImagesListViewSubItem : EXListViewSubItemAB {
        
        private ArrayList _images;
            
        public EXMultipleImagesListViewSubItem() {
            
        }
        
        public EXMultipleImagesListViewSubItem(string text) {
            this.Text = text;
        }
            
        public EXMultipleImagesListViewSubItem(ArrayList images) {
            _images = images;
        }
	
        public EXMultipleImagesListViewSubItem(ArrayList images, string value) {
            _images = images;
            this.MyValue = value;
        }
        
        public EXMultipleImagesListViewSubItem(string text, ArrayList images, string value) {
            this.Text = text;
            _images = images;
            this.MyValue = value;
        }
        
        public ArrayList MyImages {
            get {return _images;}
            set {_images = value;}
        }
        
        public override int DoDraw(DrawListViewSubItemEventArgs e, int x, EXColumnHeader ch) {    
            if (this.MyImages != null && this.MyImages.Count > 0) {
                for (int i = 0; i < this.MyImages.Count; i++) {
                    Image img = (Image) this.MyImages[i];
                    int imgy = e.Bounds.Y + ((int) (e.Bounds.Height / 2)) - ((int) (img.Height / 2));
                    e.Graphics.DrawImage(img, x, imgy, img.Width, img.Height);
                    x += img.Width + 2;
                }
            }
            return x;
        }
        
    }
    
    public class EXBoolListViewSubItem : EXListViewSubItemAB {
        
        private bool _value;
            
        public EXBoolListViewSubItem() {
            
        }
        
        public EXBoolListViewSubItem(bool val) {
            _value = val;
	        this.MyValue = val.ToString();
        }
        
        public bool BoolValue {
            get {return _value;}
            set {
		        _value = value;
		        this.MyValue = value.ToString();
	        }
        }
        
        public override int DoDraw(DrawListViewSubItemEventArgs e, int x, EXColumnHeader ch) {    
            EXBoolColumnHeader boolcol = (EXBoolColumnHeader) ch;
            Image boolimg;
            if (this.BoolValue == true) {
                boolimg = boolcol.TrueImage;
            } else {
                boolimg = boolcol.FalseImage;
            }
            int imgy = e.Bounds.Y + ((int) (e.Bounds.Height / 2)) - ((int) (boolimg.Height / 2));
            e.Graphics.DrawImage(boolimg, x, imgy, boolimg.Width, boolimg.Height);
            x += boolimg.Width + 2;
            return x;
        }
        
    }
    
    public class EXListViewItem : ListViewItem {
	
	    private string _value;
        
        public EXListViewItem() {
            
        }
        
        public EXListViewItem(string text) {
            this.Text = text;
        }
	
        public string MyValue {
            get {return _value;}
            set {_value = value;}
        }
        
    }
    
    public class EXImageListViewItem : EXListViewItem {
        
        private Image _image;
            
        public EXImageListViewItem() {
            
        }
        
        public EXImageListViewItem(string text) {
            this.Text = text;
        }
        
        public EXImageListViewItem(Image image) {
            _image = image;
        }
	
        public EXImageListViewItem(string text, Image image) {
            _image = image;
            this.Text = text;
        }
	
	    public EXImageListViewItem(string text, Image image, string value) {
            this.Text = text;
            _image = image;
	        this.MyValue = value;
        }
        
        public Image MyImage {
            get {return _image;}
            set {_image = value;}
        }
        
    }
    
    public class EXMultipleImagesListViewItem : EXListViewItem {
        
        private ArrayList _images;
            
        public EXMultipleImagesListViewItem() {
            
        }
        
        public EXMultipleImagesListViewItem(string text) {
            this.Text = text;
        }
            
        public EXMultipleImagesListViewItem(ArrayList images) {
            _images = images;
        }
	
	    public EXMultipleImagesListViewItem(string text, ArrayList images) {
            this.Text = text;
            _images = images;
        }
	
	    public EXMultipleImagesListViewItem(string text, ArrayList images, string value) {
            this.Text = text;
            _images = images;
	        this.MyValue = value;
        }
        
        public ArrayList MyImages {
            get {return _images;}
            set {_images = value;}
        }
        
    }    

}