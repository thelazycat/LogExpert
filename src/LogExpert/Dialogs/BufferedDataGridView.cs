﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using NLog;

namespace LogExpert.Dialogs
{
    public partial class BufferedDataGridView : DataGridView
    {
        #region Fields

        private static readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        //BufferedGraphics myBuffer;

        private readonly SortedList<int, BookmarkOverlay> overlayList = new SortedList<int, BookmarkOverlay>();

        private BookmarkOverlay draggedOverlay;
        private Point dragStartPoint;
        private bool isDrag = false;
        private Size oldOverlayOffset;

        #endregion

        #region cTor

        //UserControl ctl = new UserControl();


        public BufferedDataGridView()
        {
            InitializeComponent();
            this.DoubleBuffered = true;

            //ctl.Location = new Point(0, 0);
            //ctl.Size = new Size(200, 200);
            //ctl.Visible = true;
            //ctl.BackColor = Color.FromArgb(50, 100, 100, 100);
            //ctl.Parent = this;
            //this.Controls.Add(ctl);
        }

        #endregion

        #region Delegates

        public delegate void OverlayDoubleClickedEventHandler(object sender, OverlayEventArgs e);

        #endregion

        #region Events

        public event OverlayDoubleClickedEventHandler OverlayDoubleClicked;

        #endregion

        #region Properties

        /*    
      public Graphics Buffer
      {
        get { return this.myBuffer.Graphics; }
      }
       */


        public Rectangle LastRowRect { get; set; }

        public bool MustDrawFocus { get; set; }

        public ContextMenuStrip EditModeMenuStrip { get; set; } = null;

        public bool PaintWithOverlays { get; set; } = false;

        #endregion

        #region Public methods

        public void SetCurrentRow(int rowIndex)
        {
            SetCurrentCellAddressCore(0, rowIndex, true, false, false);
        }


        public void AddOverlay(BookmarkOverlay overlay)
        {
            this.overlayList.Add(overlay.Position.Y, overlay);
        }

        public CellContent GetCellContentFromPoint(int x, int y)
        {
            HitTestInfo hit = HitTest(x, y);
            if (hit.Type == DataGridViewHitTestType.Cell)
            {
                DataGridViewCellValueEventArgs args = new DataGridViewCellValueEventArgs(hit.ColumnIndex, hit.RowIndex);
                OnCellValueNeeded(args);
                for (int i = 0; i < this.ColumnCount; ++i)
                {
                    Rectangle r = this.GetColumnDisplayRectangle(i, false);
                    if (x > r.Left && x < r.Right)
                    {
                        return new CellContent(args.Value as string, r.Left);
                    }
                }
            }
            return null;
        }

        #endregion

        #region Overrides

        protected override void OnPaint(PaintEventArgs e)
        {
            try
            {
                if (PaintWithOverlays)
                {
                    PaintOverlays(e);
                }
                else
                {
                    base.OnPaint(e);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }
        }


        protected override void OnEditingControlShowing(DataGridViewEditingControlShowingEventArgs e)
        {
            base.OnEditingControlShowing(e);
            e.Control.KeyDown -= Control_KeyDown;
            e.Control.KeyDown += Control_KeyDown;
            DataGridViewTextBoxEditingControl editControl =
                (DataGridViewTextBoxEditingControl) e.Control;
            e.Control.PreviewKeyDown -= Control_PreviewKeyDown;
            e.Control.PreviewKeyDown += Control_PreviewKeyDown;

            editControl.ContextMenuStrip = this.EditModeMenuStrip;
        }


        protected override void OnMouseDown(MouseEventArgs e)
        {
            BookmarkOverlay overlay = GetOverlayForPosition(e.Location);
            if (overlay != null)
            {
                if (e.Button == MouseButtons.Right)
                {
                    if (this.isDrag)
                    {
                        this.isDrag = false;
                        overlay.Bookmark.OverlayOffset = this.oldOverlayOffset;
                        this.Refresh();
                        return;
                    }
                }
                else
                {
                    this.dragStartPoint = e.Location;
                    this.isDrag = true;
                    this.draggedOverlay = overlay;
                    this.oldOverlayOffset = overlay.Bookmark.OverlayOffset;
                }
            }
            else
            {
                this.isDrag = false;
                base.OnMouseDown(e);
            }
        }


        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (this.isDrag)
            {
                this.isDrag = false;
                this.Refresh();
            }
            else
            {
                base.OnMouseUp(e);
            }
        }


        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (this.isDrag)
            {
                this.Cursor = Cursors.Hand;
                Size offset = new Size(e.X - this.dragStartPoint.X, e.Y - this.dragStartPoint.Y);
                this.draggedOverlay.Bookmark.OverlayOffset = this.oldOverlayOffset + offset;
                this.Refresh();
            }
            else
            {
                BookmarkOverlay overlay = GetOverlayForPosition(e.Location);
                this.Cursor = overlay != null ? Cursors.Hand : Cursors.Default;
                base.OnMouseMove(e);
            }
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            BookmarkOverlay overlay = GetOverlayForPosition(e.Location);
            if (overlay != null)
            {
                if (e.Button == MouseButtons.Left)
                {
                    OnOverlayDoubleClicked(new OverlayEventArgs(overlay));
                }
            }
            else
            {
                base.OnMouseDoubleClick(e);
            }
        }

        #endregion

        #region Private Methods

        private BookmarkOverlay GetOverlayForPosition(Point pos)
        {
            lock (this.overlayList)
            {
                foreach (BookmarkOverlay overlay in this.overlayList.Values)
                {
                    if (overlay.BubbleRect.Contains(pos))
                    {
                        return overlay;
                    }
                }
            }
            return null;
        }

        #endregion

        #region Events handler

        private void Control_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if ((e.KeyCode == Keys.C || e.KeyCode == Keys.Insert) && e.Control)
            {
                if (this.EditingControl != null)
                {
                    e.IsInputKey = true;
                }
            }
        }


        private void Control_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Up || e.KeyCode == Keys.Down)
            {
                if (this.EditingControl != null)
                {
                    if (this.EditingControl.GetType().IsAssignableFrom(typeof(LogCellEditingControl)))
                    {
                        DataGridViewTextBoxEditingControl editControl =
                            this.EditingControl as DataGridViewTextBoxEditingControl;
                        editControl.EditingControlDataGridView.EndEdit();
                        int line = editControl.EditingControlDataGridView.CurrentCellAddress.Y;
                        if (e.KeyCode == Keys.Up)
                        {
                            if (line > 0)
                            {
                                line--;
                            }
                        }
                        if (e.KeyCode == Keys.Down)
                        {
                            if (line < editControl.EditingControlDataGridView.RowCount - 1)
                            {
                                line++;
                            }
                        }

                        int col = editControl.EditingControlDataGridView.CurrentCellAddress.X;
                        int scrollIndex = editControl.EditingControlDataGridView.HorizontalScrollingOffset;
                        int selStart = editControl.SelectionStart;
                        editControl.EditingControlDataGridView.CurrentCell =
                            editControl.EditingControlDataGridView.Rows[line].Cells[col];
                        editControl.EditingControlDataGridView.BeginEdit(false);
                        editControl.SelectionStart = selStart;
                        editControl.ScrollToCaret();
                        editControl.EditingControlDataGridView.HorizontalScrollingOffset = scrollIndex;
                        e.Handled = true;
                        return;
                    }
                }
            }
        }

        #endregion

        protected void PaintOverlays(PaintEventArgs e)
        {
            BufferedGraphicsContext currentContext;
            currentContext = BufferedGraphicsManager.Current;
            BufferedGraphics myBuffer = currentContext.Allocate(this.CreateGraphics(), this.ClientRectangle);


            //base.OnPaint(e);

            lock (overlayList)
            {
                overlayList.Clear();
            }
            myBuffer.Graphics.SetClip(ClientRectangle, CombineMode.Union);
            e.Graphics.SetClip(ClientRectangle, CombineMode.Union);

            PaintEventArgs args = new PaintEventArgs(myBuffer.Graphics, e.ClipRectangle);
            //PaintEventArgs args = new PaintEventArgs(myBuffer.Graphics, ClientRectangle);
            base.OnPaint(args);


            Color bubbleColor = Color.FromArgb(160, 250, 250, 00);
            Pen pen = new Pen(bubbleColor, (float) 3.0);
            Brush brush = new SolidBrush(bubbleColor);
            Brush textBrush = new SolidBrush(Color.FromArgb(200, 0, 0, 90));
            Font font = new Font("Arial", 10);
            StringFormat format = new StringFormat();
            format.LineAlignment = StringAlignment.Center;
            format.Alignment = StringAlignment.Near;

            myBuffer.Graphics.SetClip(this.DisplayRectangle, CombineMode.Intersect);

            // Spaltenheader aus Clipbereich rausnehmen
            Rectangle rectTableHeader = new Rectangle(this.DisplayRectangle.X, this.DisplayRectangle.Y,
                this.DisplayRectangle.Width, this.ColumnHeadersHeight);
            myBuffer.Graphics.SetClip(rectTableHeader, CombineMode.Exclude);

            //e.Graphics.SetClip(rect, CombineMode.Union);

            lock (this.overlayList)
            {
                foreach (BookmarkOverlay overlay in this.overlayList.Values)
                {
                    SizeF textSize = myBuffer.Graphics.MeasureString(overlay.Bookmark.Text, font, 300);
                    Rectangle rectBubble = new Rectangle(overlay.Position,
                        new Size((int) textSize.Width, (int) textSize.Height));
                    rectBubble.Offset(60, -(rectBubble.Height + 40));
                    rectBubble.Inflate(3, 3);
                    rectBubble.Location = rectBubble.Location + overlay.Bookmark.OverlayOffset;
                    overlay.BubbleRect = rectBubble;
                    myBuffer.Graphics.SetClip(rectBubble, CombineMode.Union); // Bubble to clip
                    myBuffer.Graphics.SetClip(rectTableHeader, CombineMode.Exclude);
                    e.Graphics.SetClip(rectBubble, CombineMode.Union);
                    RectangleF textRect =
                        new RectangleF(rectBubble.X, rectBubble.Y, rectBubble.Width, rectBubble.Height);
                    myBuffer.Graphics.FillRectangle(brush, rectBubble);
                    //myBuffer.Graphics.DrawLine(pen, overlay.Position, new Point(rect.X, rect.Y + rect.Height / 2));
                    myBuffer.Graphics.DrawLine(pen, overlay.Position,
                        new Point(rectBubble.X, rectBubble.Y + rectBubble.Height));
                    myBuffer.Graphics.DrawString(overlay.Bookmark.Text, font, textBrush, textRect, format);

                    if (_logger.IsDebugEnabled)
                    {
                        _logger.Debug("ClipRgn: {0},{1},{2},{3}", myBuffer.Graphics.ClipBounds.Left, myBuffer.Graphics.ClipBounds.Top, myBuffer.Graphics.ClipBounds.Width, myBuffer.Graphics.ClipBounds.Height);
                    }
                    Rectangle testRect = this.ClientRectangle;
                }
            }
            pen.Dispose();
            brush.Dispose();
            textBrush.Dispose();
            font.Dispose();

            myBuffer.Render(e.Graphics);
            myBuffer.Dispose();
        }

        protected virtual void OnOverlayDoubleClicked(OverlayEventArgs e)
        {
            if (OverlayDoubleClicked != null)
            {
                OverlayDoubleClicked(this, e);
            }
        }
    }

    public class LogGridCell : DataGridViewTextBoxCell
    {
        #region cTor

        public LogGridCell()
            : base()
        {
        }

        #endregion

        #region Properties

        public override Type EditType
        {
            get { return typeof(LogCellEditingControl); }
        }

        #endregion

        #region Public methods

        public override void InitializeEditingControl(int rowIndex, object
            initialFormattedValue, DataGridViewCellStyle dataGridViewCellStyle)
        {
            // Set the value of the editing control to the current cell value.
            base.InitializeEditingControl(rowIndex, initialFormattedValue,
                dataGridViewCellStyle);
            LogCellEditingControl ctl =
                DataGridView.EditingControl as LogCellEditingControl;
        }

        #endregion
    }


    public class LogCellEditingControl : DataGridViewTextBoxEditingControl, IDataGridViewEditingControl
    {
        #region cTor

        //bool valueChanged = false;
        //DataGridView dataGridView;
        //int rowIndex;

        public LogCellEditingControl()
            : base()
        {
        }

        #endregion

        #region Public methods

        public override bool EditingControlWantsInputKey(
            Keys key, bool dataGridViewWantsInputKey)
        {
            switch (key & Keys.KeyCode)
            {
                case Keys.Left:
                case Keys.Up:
                case Keys.Down:
                case Keys.Right:
                case Keys.Home:
                case Keys.End:
                case Keys.PageDown:
                case Keys.PageUp:
                    return true;
            }
            return !dataGridViewWantsInputKey;
        }

        #endregion

        //#region IDataGridViewEditingControl Members

        //public void ApplyCellStyleToEditingControl(DataGridViewCellStyle dataGridViewCellStyle)
        //{
        //  this.Font = dataGridViewCellStyle.Font;
        //}

        //public DataGridView EditingControlDataGridView
        //{
        //  get
        //  {
        //    return this.dataGridView;
        //  }
        //  set
        //  {
        //    this.dataGridView = value;
        //  }
        //}

        //public object EditingControlFormattedValue
        //{
        //  get
        //  {
        //    return this.Text;
        //  }
        //  set
        //  {
        //    this.Text = value as string;
        //  }
        //}

        //public int EditingControlRowIndex
        //{
        //  get
        //  {
        //    return rowIndex;
        //  }
        //  set
        //  {
        //    this.rowIndex = value;
        //  }
        //}

        //public bool EditingControlValueChanged
        //{
        //  get
        //  {
        //    return this.valueChanged;
        //  }
        //  set
        //  {
        //    this.valueChanged = value;
        //  }
        //}

        //protected override void OnTextChanged(EventArgs eventargs)
        //{
        //  // Notify the DataGridView that the contents of the cell
        //  // have changed.
        //  valueChanged = true;
        //  this.EditingControlDataGridView.NotifyCurrentCellDirty(true);
        //  base.OnTextChanged(eventargs);
        //}


        //public Cursor EditingPanelCursor
        //{
        //  get { return base.Cursor; }
        //}

        //public object GetEditingControlFormattedValue(DataGridViewDataErrorContexts context)
        //{
        //  return this.EditingControlFormattedValue;
        //}

        //public void PrepareEditingControlForEdit(bool selectAll)
        //{
        //  // nothing 
        //}

        //public bool RepositionEditingControlOnValueChange
        //{
        //  get { return false; }
        //}

        //#endregion
    }

    public class LogTextColumn : DataGridViewColumn
    {
        #region cTor

        public LogTextColumn()
            : base(new LogGridCell())
        {
        }

        #endregion
    }
}