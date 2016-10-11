using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Sender.EXControls;
using Sender.Entities;
using System.Linq.Expressions;
using System.Drawing;

namespace Sender.Extensions
{
    public static class ListViewExtensions
    {
        [DllImport("User32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        private const int LVM_SETEXTENDEDLISTVIEWSTYLE = 0x1000 + 54;
        private const int LVS_EX_HEADERDRAGDROP = 0x00000010;

        public static void SendMessage(ref Message msg)
        {
            SendMessage(msg.HWnd, msg.Msg, msg.WParam, msg.LParam);
        }

        public static void SetAllowDraggableColumns(this ListView lv, bool enabled)
        {
            // Add or remove the LVS_EX_HEADERDRAGDROP extended
            // style based upon the state of the enabled parameter.
            Message msg = new Message();
            msg.HWnd = lv.Handle;
            msg.Msg = LVM_SETEXTENDEDLISTVIEWSTYLE;
            msg.WParam = (IntPtr)LVS_EX_HEADERDRAGDROP;
            msg.LParam = enabled ? (IntPtr)LVS_EX_HEADERDRAGDROP : IntPtr.Zero;

            // Send the message to the listview control
            
            SendMessage(ref msg);
        }

        public static List<TElements> AddNewElementsToList<TElements>(this ListView listview, string[ ] filePaths, out int currentCountItemsListView)
            where TElements : class
        {
            var _lvVideoPlayer   = listview as VideoPlayerListView;
            var _lvMediaPlayer   = listview as MediaPlayerListView;
            var _lvImageListView = listview as ImageListView;
            var addedElements    = new List<TElements>();

            var lastIdTracks = -1;
            currentCountItemsListView = 0;

            if (_lvMediaPlayer != null)
            {
                foreach (var path in filePaths)
                {
                    var indexStartFileName = path.LastIndexOf(@"\") + 1;

                    if (lastIdTracks == -1)
                    {
                        currentCountItemsListView = listview.Items.Count;
                        lastIdTracks = _lvMediaPlayer.Playlist.Tracks.Max(track => (int?)track.Id) ?? 0;
                    }

                    var element = new Track(
                                        ++lastIdTracks, 
                                        path.Substring(indexStartFileName, path.Length - indexStartFileName), 
                                        path) as TElements;

                    addedElements.Add(element);
                }
            }
            else if (_lvImageListView != null)
            {
                foreach (var path in filePaths)
                {
                    var indexStartFileName = path.LastIndexOf(@"\") + 1;
                    var img = path.Substring(indexStartFileName, path.Length - indexStartFileName) as TElements;

                        // Copy
                    var fileImage = _lvImageListView.DirImageFiles + @"\" + img;

                        if (!File.Exists(fileImage))
                            File.Copy(path, _lvImageListView.DirImageFiles + @"\" + img);

                    addedElements.Add(img);
                }
            }
            else if (_lvVideoPlayer != null)
            {
                foreach (var path in filePaths)
                {
                    var indexStartFileName = path.LastIndexOf(@"\") + 1;

                    if (lastIdTracks == -1)
                    {
                        currentCountItemsListView = listview.Items.Count;
                        lastIdTracks = _lvVideoPlayer.Playlist.Tracks.Max(track => (int?)track.Id) ?? 0;
                    }

                    var element = new Track(
                                        ++lastIdTracks,
                                        path.Substring(indexStartFileName, path.Length - indexStartFileName),
                                        path) as TElements;

                    addedElements.Add(element);
                }
            }

            return addedElements;
        }

        public static void GetIndexByExpression(this ListView listview, Expression<Func<int, int, int>> expression, EventHandler buttonPlayOrPause)
        {
            var lv = listview as Player;

            if (lv == null)
                return;

            if (lv.SelectedItems.Count == 0)
                return;

            var countTracks            = lv.Playlist.TracksCount;
            var selectedItemIndex      = lv.SelectedItems[0].Index;
            var nextSelectedItemIndex  = selectedItemIndex;
            var funcIncrementDecrement = expression.Compile();
            nextSelectedItemIndex = funcIncrementDecrement(nextSelectedItemIndex, countTracks - 1);

            listview.SwapListView(nextSelectedItemIndex, selectedItemIndex);
            listview.SwapPlaylist(nextSelectedItemIndex, selectedItemIndex);

            lv.Items[nextSelectedItemIndex].Selected = true;
            lv.Select();
            lv.Items[nextSelectedItemIndex].EnsureVisible();
            lv.Items[nextSelectedItemIndex].Focused = true;
            lv.Focus();


            var currentSelectedButton = lv.GetEmbeddedControl(1, selectedItemIndex) as StartPlayMediaDataButton;
            var nextSelectedButton    = lv.GetEmbeddedControl(1, nextSelectedItemIndex) as StartPlayMediaDataButton;
            if (nextSelectedButton.ButtonState == StartPlayMediaDataButton.ButtonStates.Play && currentSelectedButton.ButtonState == StartPlayMediaDataButton.ButtonStates.Play)
            {
                if (lv.Playlist.State == PlaylistTrackStates.Pause)
                {
                    if (lv.Playlist.PreviousTrackNumber == selectedItemIndex)
                        lv.Playlist.PreviousTrackNumber = nextSelectedItemIndex;
                    else
                    {
                        lv.Playlist.PreviousTrackNumber = selectedItemIndex;
                    }
                }
                return;
            }
            else if (currentSelectedButton.ButtonState == StartPlayMediaDataButton.ButtonStates.Pause)
            {
                currentSelectedButton.ButtonState = StartPlayMediaDataButton.ButtonStates.Continue;
                buttonPlayOrPause(currentSelectedButton, new EventArgs());

                nextSelectedButton.ButtonState = StartPlayMediaDataButton.ButtonStates.Continue;
                buttonPlayOrPause(nextSelectedButton, new EventArgs());
            }
            else if (nextSelectedButton.ButtonState == StartPlayMediaDataButton.ButtonStates.Pause)
            {
                currentSelectedButton.ButtonState = StartPlayMediaDataButton.ButtonStates.Continue;
                buttonPlayOrPause(currentSelectedButton, new EventArgs());
            }
        }

        public static void SwapListView(this ListView listview, int previousElemIndex, int nextElemIndex)
        {
            var prevItemText = listview.Items[previousElemIndex].Text;
            var nextItemText = listview.Items[nextElemIndex].Text;

            listview.Items[previousElemIndex].Text = nextItemText;
            listview.Items[nextElemIndex].Text = prevItemText;
        }

        public static void SwapPlaylist(this ListView listview, int previousElemIndex, int nextElemIndex)
        {
            var lv = listview as Player;

            if (lv == null)
                return;

            var prevTrack = lv.Playlist.Tracks[previousElemIndex];
            var nextTrack = lv.Playlist.Tracks[nextElemIndex];

            lv.Playlist.Tracks[previousElemIndex] = nextTrack;
            lv.Playlist.Tracks[nextElemIndex]     = prevTrack;
        }

        public static void ColorItemForPlayTrack(this ListView listview, int numberTrack)
        {
            if (numberTrack != -1)
            {
                listview.Items[numberTrack].BackColor = Color.Green;
                listview.Items[numberTrack].ForeColor = Color.White;
                listview.Items[numberTrack].Font      = new Font("Times New Roman", 13);
            }
        }

        public static void ColorItemForNonPlayTrack(this ListView listview, int numberTrack)
        {
            if (numberTrack != -1)
            {
                listview.Items[numberTrack].BackColor = Color.White;
                listview.Items[numberTrack].ForeColor = Color.Black;
                listview.Items[numberTrack].Font      = new Font("Times New Roman", 12);
            }
        }

        // it's a bad code
        public static void RecountButtonNumbers(this ListViewEx listview, int startRow, int sourceCountElements)
        {
            for (int row = startRow + 1; row < sourceCountElements; row++)                              // + 1, because we don't consider the row which was deleted
            {
                for (int col = 1; col < listview.Columns.Count - 1; col++)
                {
                    if (col == 1)                                                                       // The Play button
                    {
                        var but = listview.GetEmbeddedControl(col, row) as StartPlayMediaDataButton;
                        but.NumberRowItem = row - 1;
                        listview.RemoveEmbeddedControl(listview.GetEmbeddedControl(col, row));
                        listview.AddEmbeddedControl(but, col, row - 1);
                    }
                    else                                                                                // The Delete button
                    {
                        var but = listview.GetEmbeddedControl(col, row) as DeleteButton;
                        but.NumberRowDeletedItem = row - 1;
                        listview.RemoveEmbeddedControl(listview.GetEmbeddedControl(col, row));
                        listview.AddEmbeddedControl(but, col, row - 1);
                    }
                }
            }
        }
    }
}
