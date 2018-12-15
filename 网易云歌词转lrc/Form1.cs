using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Drawing;

namespace 云音乐歌词转lrc
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            //this.MinimumSize = Size;
            lstDir.Items.Clear();
            string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            lstDir.Items.Add(local + @"\Packages\1F8B0F94.122165AE053F_j2p0p5q0044a6\LocalCache\cache\lyric");
            lstDir.Items.Add(local + @"\Packages\1F8B0F94.122165AE053F_j2p0p5q0044a6\LocalState\download\lyric");
            txtOD.Text = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            UpdateFileList();

            //AutoSetMinisize();
            //Graphics g = Graphics.FromHwnd(IntPtr.Zero);
            //float dpiX = g.DpiX;
            //float dpiY = g.DpiY;
            //Size b = this.Size - this.ClientSize;
            //var l = new Size((int)(800 * dpiX / 96), (int)(600 * dpiY / 96));
            //this.Size = l + b;
        }

        private const string 文件过大提示 = "当前文件过大（大于40KB），请确认选择了正确的文件";
        private const string Loading = "正在读取文件，请稍后";

        private bool lyricIsChanged = false; // 歌词已经改变

        protected FileInfo LastSuccessfulSelection { get; private set; } // 上一个正确的选择项

        private Lyric currentLyric; // 当前歌词

        // 此方法用来将某个目录中的文件添加至文件列表，不会删除重复内容，
        // 也不会添加无歌词的云音乐歌词文件
        private void AddDirectory(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                MessageBox.Show("正在试图添加一个不存在的目录");
                return;
            }

            #region 添加文件到列表
            var dir = new DirectoryInfo(directoryPath);
            var files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                if (file.Length < 100)
                    continue;
                lstFile.Items.Add(file); // 先试试行不行，如果出现错误，尝试 file.Name
            }
            #endregion

            #region 添加目录（视情况处理）
            bool directoryNotAddedToListbox = true;
            foreach (string directory in lstDir.Items)
            {
                if (directory == directoryPath)
                {
                    //MessageBox.Show("当前选择的目录已在列表当中");
                    //return;
                    directoryNotAddedToListbox = false;
                    break;
                }
            }
            if (directoryNotAddedToListbox)
                lstDir.Items.Add(directoryPath);
            #endregion
        }

        // 刷新文件列表，并移除失效目录，然后重新定位至上次选定的文件
        private void UpdateFileList()
        {
            //if (保持当前编辑好的歌词)
            //{
            //    Lyric temp = currentLyric;
            //    lyricIsChanged = false;
            //    UpdateFileList();
            //    currentLyric = temp;
            //    lyricIsChanged = true;
            //}
            lstFile.Items.Clear();

            ArrayList unexistedDirs = new ArrayList();
            foreach (string dirPath in lstDir.Items)
            {
                if (Directory.Exists(dirPath))
                    AddDirectory(dirPath);
                else
                    unexistedDirs.Add(dirPath);
            }
            foreach (string dirPath in unexistedDirs)
            {
                lstDir.Items.Remove(dirPath);
            }

            if (LastSuccessfulSelection != null)
            {
                foreach (FileInfo @new in lstFile.Items)
                {
                    if (LastSuccessfulSelection.FullName == @new.FullName)
                    {
                        lstFile.SelectedItem = @new;
                        return;
                    }
                }
                LastSuccessfulSelection = null;
            }
        }


        #region Form
        // 搜索框文本更改
        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            if (txtSearch.Text == "")
                UpdateFileList();
        }

        // 在搜索框按回车（进行搜索）
        private void txtSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                UpdateFileList();

                List<FileInfo> searchResult = new List<FileInfo>();
                foreach (FileInfo file in lstFile.Items)
                {
                    if (file.Length <= 40960)
                    {
                        var fullLyric = new StreamReader(file.FullName).ReadToEnd();
                        if (fullLyric.Contains(txtSearch.Text))
                            searchResult.Add(file);
                    }
                }

                lstFile.Items.Clear();
                foreach (FileInfo result in searchResult)
                {
                    lstFile.Items.Add(result);
                    if (result.FullName == LastSuccessfulSelection?.FullName)
                        lstFile.SelectedItem = result; // 恢复之前选择的文件
                }
            }
        }

        // 添加来源目录
        private void btnAddDir_Click(object sender, EventArgs e)
        {
            var folderBrowserDialog1 = new FolderBrowserDialog();
            folderBrowserDialog1.ShowNewFolderButton = false;
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                foreach (string directory in lstDir.Items)
                {
                    if (directory == folderBrowserDialog1.SelectedPath)
                    {
                        MessageBox.Show("当前选择的目录已在列表当中");
                        return;
                    }
                }
                lstDir.Items.Add(folderBrowserDialog1.SelectedPath);
                AddDirectory(folderBrowserDialog1.SelectedPath);
            }
        }

        // 设置输出目录和文件
        private void button1_Click(object sender, EventArgs e)
        {
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                FileInfo fi = new FileInfo(saveFileDialog1.FileName);
                txtOD.Text = fi.DirectoryName;
                txtFileName.Text = fi.Name;
            }
        }

        //“生成”按钮
        private void button2_Click(object sender, EventArgs e)
        {
            if (currentLyric != null)
            {
                if (txtFileName.Text.StartsWith(".") ||
                    txtFileName.Text.Length == 0)
                {
                    MessageBox.Show("请确保输入文件名");
                    return;
                }

                string path = txtOD.Text;
                if (!path.EndsWith("\\"))
                    path += "\\";
                txtFileName.Text = txtFileName.Text.TrimEnd('.');
                if (!txtFileName.Text.Contains(".")) txtFileName.Text += ".lrc";
                path += txtFileName.Text;
                if (File.Exists(path) && MessageBox.Show("当前文件已存在，确定覆盖吗？",
                    "是否覆盖", MessageBoxButtons.YesNo) == DialogResult.No)
                {
                    return;
                }
                try
                {
                    File.WriteAllText(path, currentLyric.lyric);
                    MessageBox.Show("歌词已经转换到" + path);
                }
                catch
                {
                    MessageBox.Show("请确保输入正确的文件名！，并确保拥有输出目录的写入权限");
                }
            }
            else MessageBox.Show("请选择一个正确的文件");
        }

        // 选择项更改时，自动更新预览
        private void lstFile_SelectedValueChanged(object sender, EventArgs e)
        {
            var selectedFile = lstFile.SelectedItem as FileInfo;

            // 如果新选择的项目和已经选择的项目相同，或者新项目为 null（未选择任何项目时点击左侧空白处会发生），则不触发
            if (selectedFile is null
                ||
                LastSuccessfulSelection != null &&
                selectedFile.FullName == LastSuccessfulSelection.FullName)
                return;

            if (File.Exists(selectedFile.FullName))
            {
                if (!(lyricIsChanged && MessageBox.Show("歌词已经改变，要放弃更改吗？",
                    "是否放弃更改", MessageBoxButtons.YesNo) == DialogResult.No))
                {// 未更改或放弃更改
                    lyricIsChanged = false;
                    if (selectedFile.Length <= 40960)
                    {
                        currentLyric = new Lyric(selectedFile);
                        txtLyric.Text = currentLyric.lyric;
                    }
                    else
                    {
                        txtLyric.Text = 文件过大提示;
                        currentLyric = null;
                    }
                    LastSuccessfulSelection = (FileInfo)lstFile.SelectedItem;
                }
                else
                {// 不放弃更改
                    lstFile.SelectedItem = LastSuccessfulSelection;
                    //UpdateFileList(true); // 如使用此行，不放弃更改则会刷新文件列表
                }
            }
            else
            {
                MessageBox.Show("文件不存在，将刷新文件列表");
                UpdateFileList();
            }
        }

        // 双击歌词预览编辑歌词
        private void txtLyric_DoubleClick(object sender, EventArgs e)
        {
            if (currentLyric != null)
            {
                var editForm = new Editor(currentLyric);
                editForm.ShowDialog();

                if (editForm.IsChanged)
                {
                    currentLyric.lyric = editForm.Lyric;
                    if (currentLyric.lyric == currentLyric.startLyric)
                        lyricIsChanged = false;
                    else
                        lyricIsChanged = true;

                    txtLyric.Text = currentLyric.lyric;
                }
            }
        }
        #endregion

        #region Size
        private void AutoSetMinisize()
        {
            Graphics g = Graphics.FromHwnd(IntPtr.Zero);
            float dpiX = g.DpiX;
            float dpiY = g.DpiY;
            double ox = 500;
            double oy = 350;
            Size b = this.Size - this.ClientSize;
            Size l = new Size((int)(ox * dpiX / 96), (int)(oy * dpiY / 96));
            this.MinimumSize = l + b;
        }

        private void Form1_Move(object sender, EventArgs e)
        {
            //AutoSetMinisize();
        }
        #endregion
    }

    public class Lyric
    {
        public Lyric() { }
        public Lyric(string lyric)
        {
            startLyric = new LyricConverter(lyric).Get();
            if (string.IsNullOrWhiteSpace(startLyric))
            {
                startLyric = lyric;
                lyricType = LyricType.Other;
            }
            this.lyric = startLyric;
        }
        public Lyric(FileInfo file, string lyric)
        {
            startLyric = new LyricConverter(lyric).Get();
            if (string.IsNullOrWhiteSpace(startLyric))
            {
                startLyric = lyric;
                lyricType = LyricType.Other;
            }
            this.lyric = startLyric;
            File = file;
        }
        public Lyric(FileInfo file)
        {
            string lyric = new StreamReader(file.FullName).ReadToEnd();
            startLyric = new LyricConverter(lyric).Get();
            if (string.IsNullOrWhiteSpace(startLyric))
            {
                startLyric = lyric;
                lyricType = LyricType.Other;
            }
            this.lyric = startLyric;
        }

        public readonly string startLyric; // 初始歌词
        public string lyric; // 当前歌词

        //LyricType startLyricType = LyricType.云音乐格式; // 初始的歌词类型
        LyricType lyricType = LyricType.Lrc; // 当前歌词类型

        // 返回当前实例的歌词格式（施工中）
        public LyricType GetLyricType() { return lyricType; }

        // 歌词是否改变
        public bool IsChanged
        {
            get
            {
                if (lyric == startLyric)
                    return false;
                else
                    return true;
            }
        }

        // 歌词文件的 FileInfo
        public FileInfo File { get; private set; }

        // 将云音乐歌词转换为.lrc格式
        public static string ToLrc(string 云音乐歌词)
        {
            return new LyricConverter(云音乐歌词).Get();
        }

        public enum LyricType // 指示当前歌词的类型
        {
            云音乐格式 = 0,
            Lrc = 1,
            Other = -1
        }

    }

    //Netease

    class LyricConverter
    {
        public LyricConverter(string s, bool message = true)
        {
            if (s.Trim().StartsWith("["))
                lyric = s;
            else if (s.Trim().StartsWith("{"))
            {
                lyric = s.Trim();
                ignoreIndex = new List<int>();
                searchIndex = 0;
                while (searchIndex != -1)
                {
                    ignoreIndex.Add(GetNextIndex('\\'));
                }
                ignoreIndex.Remove(-1);
                searchIndex = 0;
                List<CloudMusicInfo> lyricInfo = GetLyricInfo();
                try
                {
                    //if (!(GetVal(lyricInfo, "nolyric") || GetVal(lyricInfo, "uncollected")))
                    if (GetVal(lyricInfo, "lrc") != null &&
                    GetVal(GetVal(lyricInfo, "lrc"), "version") > 0)
                    {
                        string ol = Escape(GetVal(GetVal(lyricInfo, "lrc"), "lyric"));

                        string[] newline = { Environment.NewLine };
                        string[] olg = ol.Split(newline, StringSplitOptions.RemoveEmptyEntries);

                        List<string> total = new List<string>();

                        if (GetVal(GetVal(lyricInfo, "tlyric"), "version") > 0)
                        {
                            #region translate
                            string tl = Escape(GetVal(GetVal(lyricInfo, "tlyric"), "lyric"));

                            string[] tlg = tl.Split(newline, StringSplitOptions.RemoveEmptyEntries);

                            List<string> olgd = Expand(olg);
                            List<string> tlgd = Expand(tlg);


                            int n = olgd.Count();
                            int nt = tlgd.Count();
                            int i = 0, it = 0;

                            //List<string> oinfo = new List<string>();
                            //List<string> tinfo = new List<string>();

                            while (i < n && it < nt)
                            {
                                LyricLine o = new LyricLine(olgd[i]);
                                LyricLine t = new LyricLine(tlgd[it], true);
                                if (!o.IsLyric)
                                {
                                    i++;
                                    total.Add(o.GetInfo());
                                    continue;
                                }
                                if (!t.IsLyric)
                                {
                                    it++;
                                    total.Add(t.GetInfo());
                                    continue;
                                }
                                if (o.IsLyric && t.IsLyric)
                                {
                                    if (o.GetInfo().Contains("-"))
                                    {
                                        i++;
                                        continue;
                                    }
                                    if (t.GetInfo().Contains("-"))
                                    {
                                        it++;
                                        continue;
                                    }

                                    if (string.Compare(o.GetInfo(), t.GetInfo()) > 0)
                                    {
                                        it++;
                                        total.Add(t.GetInfo() + "译：" + t.Lyric);
                                    }
                                    else if (string.Compare(o.GetInfo(), t.GetInfo()) < 0)
                                    {
                                        total.Add(o.GetInfo() + o.Lyric);
                                        i++;
                                    }
                                    else
                                    {
                                        if (string.IsNullOrWhiteSpace(o.Lyric) ||
                                            string.IsNullOrWhiteSpace(t.Lyric))
                                            total.Add(o.GetInfo() + o.Lyric + t.Lyric);
                                        else if (o.Lyric != t.Lyric)
                                            total.Add(o.GetInfo() + o.Lyric + "/" + t.Lyric);
                                        else total.Add(o.GetInfo() + o.Lyric);

                                        i++;
                                        it++;
                                    }
                                }
                            }
                            #endregion
                        }
                        else
                        {// 保证秒的小数点后面是两位
                            foreach (string line in olg)
                            {
                                LyricLine o = new LyricLine(line);
                                total.Add(o.GetInfo() + o.Lyric);
                            }
                        }

                        lyric = RemoveRepeatedLinesAndConnect(total);
                    }
                }
                catch
                {
                    //if (message) MessageBox.Show("请检查歌词文件"
                    //+ Environment.NewLine + s
                    //);
                    lyric = null;
                }
            }
            else lyric = null;
        }

        private string lyric;
        //private List<LyricLine> lyricLines;
        private int searchIndex;
        private readonly static char[] translateTrimChars = { '〖', '〗', '【', '】' };
        private readonly static char[] endChar = { ',', '}' };
        private List<int> ignoreIndex;

        public string Get() { return lyric; }

        private int GetNextIndex(char c)
        {
            if (searchIndex == -1)
                searchIndex = 0;

            searchIndex = lyric.IndexOf(c, searchIndex);
            int temp = searchIndex;
            if (searchIndex >= 0)
            {
                searchIndex++;
                if (ignoreIndex.Contains(temp - 1))
                    return GetNextIndex(c);
            }
            return temp;
        }
        private int GetNextIndex(char[] c)
        {
            if (searchIndex == -1)
                searchIndex = 0;

            searchIndex = lyric.IndexOfAny(c, searchIndex);
            int temp = searchIndex;
            if (searchIndex >= 0)
            {
                searchIndex++;
                if (ignoreIndex.Contains(temp - 1))
                    return GetNextIndex(c);
            }
            return temp;
        }
        private List<CloudMusicInfo> GetLyricInfo()
        {
            if (lyric[searchIndex] != '{')
            {
                MessageBox.Show("歌词解析错误");
                return null;
            }

            var r = new List<CloudMusicInfo>();
            //searchIndex = 0;
            while (searchIndex != -1)
            {
                int s = GetNextIndex('\"');
                int e = GetNextIndex('\"');

                if (lyric[e + 1] != ':' || s == -1 || e == -1)
                {
                    MessageBox.Show("歌词解析错误");
                    if (s == -1 || e == -1)
                        searchIndex = -1;
                    return null;
                }

                CloudMusicInfo temp = new CloudMusicInfo();
                temp.描述 = lyric.Substring(s + 1, e - s - 1);
                try
                {
                    switch (lyric[e + 2])
                    {
                        case '\"':
                            s = GetNextIndex('\"');
                            e = GetNextIndex('\"');
                            temp.值 = lyric.Substring(s + 1, e - s - 1);
                            break;
                        case '{':
                            if (GetNextIndex('{') != -1)
                                searchIndex--;
                            temp.值 = GetLyricInfo();
                            break;
                        case 'f':
                            if (lyric.Substring(e + 2, 5) == "false")
                                temp.值 = false;
                            else
                                MessageBox.Show("在查找false时错误");
                            break;
                        case 't':
                            if (lyric.Substring(e + 2, 4) == "true")
                                temp.值 = true;
                            else
                                MessageBox.Show("在查找true时错误");
                            break;
                        default: // int或数字
                            int n = lyric.IndexOfAny(endChar, searchIndex);
                            long parse;
                            if (!long.TryParse(lyric.Substring(e + 2, n - e - 2), out parse))
                                MessageBox.Show("整数转换错误，原始数据是：" + lyric.Substring(e + 2, n - e - 2));
                            temp.值 = parse;
                            break;
                    }
                }
                catch (Exception) { MessageBox.Show("未知错误"); }
                r.Add(temp);
                if (lyric[GetNextIndex(endChar)] == '}')
                    break;
            }
            return r;
        }
        private static dynamic GetVal(List<CloudMusicInfo> l, string s)
        {
            foreach (CloudMusicInfo item in l)
            {
                if (item.描述 == s)
                { return item.值; }
            }
            return null;
        }
        private static string Escape(string s)
        {
            return s.Replace(@"\\", @"\92").Replace(@"\n", Environment.NewLine).Replace("\\\"", "\"").Replace(@"\92", @"\");
        }
        private static string RemoveRepeatedLinesAndConnect(List<string> lyriclines)
        {
            for (int i = 0; i < lyriclines.Count; i++)
            {
                int p;
                while ((p = lyriclines.LastIndexOf(lyriclines[i])) != i)
                    lyriclines.RemoveAt(p);
            }
            return string.Join(Environment.NewLine, lyriclines);
        }
        private static List<string> Expand(string[] lyriclines)
        {
            List<string> noLrc = new List<string>();
            List<string> lyricList = new List<string>();
            List<string> extra = new List<string>();
            foreach (string line in lyriclines)
            {
                var lineInfo = new LyricLine(line);
                if (lineInfo.IsLyric)
                {
                    string lyricText;
                    List<string> timeLines = GetAllTimeLine(line, out lyricText);
                    //int n = timeLines.Count;
                    //for (int i = 0; i < n; i++)
                    //{
                    //    extra.Add(timeLines[i] + lyricText);
                    //}
                    foreach (string timeLine in timeLines)
                    {
                        extra.Add(timeLine + lyricText);
                    }
                }
                else noLrc.Add(line);
            }

            foreach (string extraline in extra)
            {
                int n = lyricList.Count;
                int i;
                for (i = 0; i < n; i++)
                {
                    if (string.Compare(extraline, lyricList[i]) < 0)
                    {
                        lyricList.Insert(i, extraline);
                        break;
                    }
                }
                if (i == n)
                {
                    lyricList.Add(extraline);
                }
            }

            lyricList.InsertRange(0, noLrc);

            return lyricList;
        }
        private static List<string> GetAllTimeLine(string line, out string lyric)
        {
            var list = new List<string>();
            var lineInfo = new LyricLine(line);
            if (lineInfo.IsLyric)
            {
                list.Add(lineInfo.GetInfo());
                list.AddRange(GetAllTimeLine(lineInfo.Lyric, out lyric));
            }
            else lyric = line;
            return list;
        }

        // 云音乐的单条信息
        private struct CloudMusicInfo
        {
            public string 描述;
            public dynamic 值;
        }

        // lrc歌词行（包括时间轴和歌词），与云音乐无关
        private struct LyricLine
        {
            enum LyricLineType
            {
                Lyric = 0,
                Info = 1,
                Other = 2
            }

            private string info;
            private string lyric;
            private LyricLineType type;

            public LyricLine(string l, bool translateTrimer = false)
            {
                int e = l.IndexOf(']');
                if (e == -1)
                {
                    type = LyricLineType.Other;
                    info = "";
                    lyric = l;
                    return;
                }
                info = l.Substring(0, e + 1);
                int temp;
                try
                {
                    if (int.TryParse(l.Substring(1, 2), out temp))
                        type = LyricLineType.Lyric;
                    else
                        type = LyricLineType.Info;
                }
                catch
                {
                    type = LyricLineType.Other;
                    info = "";
                    lyric = l;
                }

                lyric = l.Substring(e + 1);
                lyric = lyric.Trim();
                if (translateTrimer)
                    lyric = lyric.Trim(translateTrimChars);
            }

            public string GetInfo(int 秒后位数 = 2)
            {
                if (秒后位数 == 2)
                    if (type == LyricLineType.Lyric)
                    {
                        try
                        {
                            return info.Substring(0, 9) + "]";
                        }
                        catch { return info; }
                    }
                    else
                        return info;
                else if (秒后位数 == 3 && type == LyricLineType.Lyric)
                    return info;
                else
                    return null;
            }
            public bool IsLyric
            {
                get
                {
                    return type == LyricLineType.Lyric ? true : false;
                }
            }
            public string Lyric { get { return lyric; } }
        }
    }
}