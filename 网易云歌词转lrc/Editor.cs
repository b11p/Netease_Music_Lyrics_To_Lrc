using System;
using System.Windows.Forms;

namespace 云音乐歌词转lrc
{
    public partial class Editor : Form
    {
        public Editor(Lyric _lyric)
        {
            InitializeComponent();
            lyric = _lyric;
            startLyric = _lyric.lyric;
            txtLyrics.Text = _lyric.lyric;
        }

        private readonly string startLyric; // 实例化此窗口时传入的歌词

        Lyric lyric; // 歌词

        // 将当前的歌词更新至 lyric
        void UpdateLyric()
        {
            lyric.lyric = txtLyrics.Text;
        }

        // 返回歌词是否在此编辑窗口中改变
        // 注意：不会比较歌词与初始状态是否不同
        public bool IsChanged
        {
            get
            {
                UpdateLyric();
                if (startLyric == lyric.lyric)
                {
                    return false;
                }
                else return true;
            }
        }

        // 返回当前编辑好的歌词
        public string Lyric
        { get { UpdateLyric(); return lyric.lyric; } }

        private void txtLyrics_TextChanged(object sender, EventArgs e)
        { }

        private void btnOK_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
