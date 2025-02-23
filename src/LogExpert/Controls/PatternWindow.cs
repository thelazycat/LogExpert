using LogExpert.Classes;
using LogExpert.Dialogs;
using LogExpert.Entities.EventArgs;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace LogExpert.Controls
{
    public partial class PatternWindow : Form
    {
        #region Fields

        private readonly List<List<PatternBlock>> _blockList = [];
        private PatternBlock _currentBlock;
        private List<PatternBlock> currentList;

        private readonly LogWindow.LogWindow _logWindow;
        private PatternArgs _patternArgs = new();

        #endregion

        #region cTor

        public PatternWindow()
        {
            InitializeComponent();
        }

        public PatternWindow(LogWindow.LogWindow logWindow)
        {
            _logWindow = logWindow;
            InitializeComponent();
            recalcButton.Enabled = false;
        }

        #endregion

        #region Properties

        public int Fuzzy
        {
            set { _fuzzyKnobControl.Value = value; }
            get { return _fuzzyKnobControl.Value; }
        }

        public int MaxDiff
        {
            set { _maxDiffKnobControl.Value = value; }
            get { return _maxDiffKnobControl.Value; }
        }

        public int MaxMisses
        {
            set { _maxMissesKnobControl.Value = value; }
            get { return _maxMissesKnobControl.Value; }
        }

        public int Weight
        {
            set { _weigthKnobControl.Value = value; }
            get { return _weigthKnobControl.Value; }
        }

        #endregion

        #region Public methods

        public void SetBlockList(List<PatternBlock> flatBlockList, PatternArgs patternArgs)
        {
            _patternArgs = patternArgs;
            _blockList.Clear();
            List<PatternBlock> singeList = [];
            //int blockId = -1;
            for (int i = 0; i < flatBlockList.Count; ++i)
            {
                PatternBlock block = flatBlockList[i];
                singeList.Add(block);
                //if (block.blockId != blockId)
                //{
                //  singeList = new List<PatternBlock>();
                //  PatternBlock selfRefBlock = new PatternBlock();
                //  selfRefBlock.targetStart = block.startLine;
                //  selfRefBlock.targetEnd = block.endLine;
                //  selfRefBlock.blockId = block.blockId;
                //  singeList.Add(selfRefBlock);
                //  singeList.Add(block);
                //  this.blockList.Add(singeList);
                //  blockId = block.blockId;
                //}
                //else
                //{
                //  singeList.Add(block);
                //}
            }
            _blockList.Add(singeList);
            Invoke(new MethodInvoker(SetBlockListGuiStuff));
        }


        public void SetColumnizer(ILogLineColumnizer columnizer)
        {
            _logWindow.SetColumnizer(columnizer, patternHitsDataGridView);
            _logWindow.SetColumnizer(columnizer, contentDataGridView);
            patternHitsDataGridView.Columns[0].Width = 20;
            contentDataGridView.Columns[0].Width = 20;

            DataGridViewTextBoxColumn blockInfoColumn = new();
            blockInfoColumn.HeaderText = "Weight";
            blockInfoColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.NotSet;
            blockInfoColumn.Resizable = DataGridViewTriState.False;
            blockInfoColumn.DividerWidth = 1;
            blockInfoColumn.ReadOnly = true;
            blockInfoColumn.Width = 50;

            DataGridViewTextBoxColumn contentInfoColumn = new();
            contentInfoColumn.HeaderText = "Diff";
            contentInfoColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.NotSet;
            contentInfoColumn.Resizable = DataGridViewTriState.False;
            contentInfoColumn.DividerWidth = 1;
            contentInfoColumn.ReadOnly = true;
            contentInfoColumn.Width = 50;

            patternHitsDataGridView.Columns.Insert(1, blockInfoColumn);
            contentDataGridView.Columns.Insert(1, contentInfoColumn);
        }

        public void SetFont(string fontName, float fontSize)
        {
            Font font = new(new FontFamily(fontName), fontSize);
            int lineSpacing = font.FontFamily.GetLineSpacing(FontStyle.Regular);
            float lineSpacingPixel = font.Size * lineSpacing / font.FontFamily.GetEmHeight(FontStyle.Regular);

            patternHitsDataGridView.DefaultCellStyle.Font = font;
            contentDataGridView.DefaultCellStyle.Font = font;
            //this.lineHeight = font.Height + 4;
            patternHitsDataGridView.RowTemplate.Height = font.Height + 4;
            contentDataGridView.RowTemplate.Height = font.Height + 4;
        }

        #endregion

        #region Private Methods

        private void SetBlockListGuiStuff()
        {
            patternHitsDataGridView.RowCount = 0;
            blockCountLabel.Text = "0";
            contentDataGridView.RowCount = 0;
            blockLinesLabel.Text = "0";
            recalcButton.Enabled = true;
            setRangeButton.Enabled = true;
            if (_blockList.Count > 0)
            {
                SetCurrentList(_blockList[0]);
            }
        }

        private void SetCurrentList(List<PatternBlock> patternList)
        {
            patternHitsDataGridView.RowCount = 0;
            currentList = patternList;
            patternHitsDataGridView.RowCount = currentList.Count;
            patternHitsDataGridView.Refresh();
            blockCountLabel.Text = "" + currentList.Count;
        }

        private int GetLineForHitGrid(int rowIndex)
        {
            int line;
            line = currentList[rowIndex].targetStart;
            return line;
        }

        private int GetLineForContentGrid(int rowIndex)
        {
            int line;
            line = _currentBlock.targetStart + rowIndex;
            return line;
        }

        #endregion

        #region Events handler

        private void OnPatternHitsDataGridViewCellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
        {
            if (currentList == null || e.RowIndex < 0)
            {
                return;
            }
            int rowIndex = GetLineForHitGrid(e.RowIndex);
            int colIndex = e.ColumnIndex;
            if (colIndex == 1)
            {
                e.Value = currentList[e.RowIndex].weigth;
            }
            else
            {
                if (colIndex > 1)
                {
                    colIndex--; // correct the additional inserted col
                }
                e.Value = _logWindow.GetCellValue(rowIndex, colIndex);
            }
        }

        private void OnPatternHitsDataGridViewCellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (currentList == null || e.RowIndex < 0)
            {
                return;
            }

            if (e.ColumnIndex == 1)
            {
                e.PaintBackground(e.CellBounds, false);
                int selCount = _patternArgs.endLine - _patternArgs.startLine;
                int maxWeight = _patternArgs.maxDiffInBlock * selCount + selCount;

                if (maxWeight > 0)
                {
                    int width = (int)((int)e.Value / (double)maxWeight * e.CellBounds.Width);
                    Rectangle rect = new(e.CellBounds.X, e.CellBounds.Y, width, e.CellBounds.Height);
                    int alpha = 90 + (int)((int)e.Value / (double)maxWeight * 165);
                    Color color = Color.FromArgb(alpha, 170, 180, 150);
                    Brush brush = new SolidBrush(color);
                    rect.Inflate(-2, -1);
                    e.Graphics.FillRectangle(brush, rect);
                    brush.Dispose();
                }
                e.PaintContent(e.CellBounds);
                e.Handled = true;
            }
            else
            {
                BufferedDataGridView gridView = (BufferedDataGridView)sender;
                int rowIndex = GetLineForHitGrid(e.RowIndex);
                _logWindow.CellPainting(gridView, rowIndex, e);
            }
        }

        private void OnPatternHitsDataGridViewMouseDoubleClick(object sender, MouseEventArgs e)
        {
            //if (this.currentList == null || patternHitsDataGridView.CurrentRow == null)
            //  return;
            //int rowIndex = GetLineForHitGrid(patternHitsDataGridView.CurrentRow.Index);

            //this.logWindow.SelectLogLine(rowIndex);
        }

        private void OnPatternHitsDataGridViewCurrentCellChanged(object sender, EventArgs e)
        {
            if (currentList == null || patternHitsDataGridView.CurrentRow == null)
            {
                return;
            }
            if (patternHitsDataGridView.CurrentRow.Index > currentList.Count - 1)
            {
                return;
            }
            contentDataGridView.RowCount = 0;
            _currentBlock = currentList[patternHitsDataGridView.CurrentRow.Index];
            contentDataGridView.RowCount = _currentBlock.targetEnd - _currentBlock.targetStart + 1;
            contentDataGridView.Refresh();
            contentDataGridView.CurrentCell = contentDataGridView.Rows[0].Cells[0];
            blockLinesLabel.Text = "" + contentDataGridView.RowCount;
        }

        private void OnContentDataGridViewCellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
        {
            if (_currentBlock == null || e.RowIndex < 0)
            {
                return;
            }

            int rowIndex = GetLineForContentGrid(e.RowIndex);
            int colIndex = e.ColumnIndex;

            if (colIndex == 1)
            {
                QualityInfo qi;
                if (_currentBlock.qualityInfoList.TryGetValue(rowIndex, out qi))
                {
                    e.Value = qi.quality;
                }
                else
                {
                    e.Value = "";
                }
            }
            else
            {
                if (colIndex != 0)
                {
                    colIndex--; // adjust the inserted column
                }
                e.Value = _logWindow.GetCellValue(rowIndex, colIndex);
            }
        }

        private void OnContentDataGridViewCellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (_currentBlock == null || e.RowIndex < 0)
            {
                return;
            }
            BufferedDataGridView gridView = (BufferedDataGridView)sender;
            int rowIndex = GetLineForContentGrid(e.RowIndex);
            _logWindow.CellPainting(gridView, rowIndex, e);
        }

        private void OnContentDataGridViewCellMouseDoubleClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (_currentBlock == null || contentDataGridView.CurrentRow == null)
            {
                return;
            }
            int rowIndex = GetLineForContentGrid(contentDataGridView.CurrentRow.Index);

            _logWindow.SelectLogLine(rowIndex);
        }

        private void OnRecalcButtonClick(object sender, EventArgs e)
        {
            _patternArgs.fuzzy = _fuzzyKnobControl.Value;
            _patternArgs.maxDiffInBlock = _maxDiffKnobControl.Value;
            _patternArgs.maxMisses = _maxMissesKnobControl.Value;
            _patternArgs.minWeight = _weigthKnobControl.Value;
            _logWindow.PatternStatistic(_patternArgs);
            recalcButton.Enabled = false;
            setRangeButton.Enabled = false;
        }

        private void OnCloseButtonClick(object sender, EventArgs e)
        {
            Close();
        }

        private void OnContentDataGridViewColumnDividerDoubleClick(object sender,
            DataGridViewColumnDividerDoubleClickEventArgs e)
        {
            e.Handled = true;
            contentDataGridView.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
        }

        private void OnPatternHitsDataGridViewColumnDividerDoubleClick(object sender,
            DataGridViewColumnDividerDoubleClickEventArgs e)
        {
            e.Handled = true;
            patternHitsDataGridView.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
        }

        private void OnSetRangeButtonClick(object sender, EventArgs e)
        {
            _logWindow.PatternStatisticSelectRange(_patternArgs);
            recalcButton.Enabled = true;
            rangeLabel.Text = "Start: " + _patternArgs.startLine + "\r\nEnd: " + _patternArgs.endLine;
        }

        #endregion
    }
}