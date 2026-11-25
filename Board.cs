using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Threading;

public class Note {
    public string Title {get;set;}
    public int Group {get;set;}
    public DateTime? Due {get;set;}
    public bool Done {get;set;}
}

public class Board : Form {
    static readonly string[] Names = {"重要且紧急","重要不紧急","紧急不重要","不紧急不重要"};
    static readonly string[] Colors = {"#F8DED4","#E0EADB","#F5E9C9","#E2E6F2"};
    static readonly Color Ink = ColorTranslator.FromHtml("#30372F");
    static readonly string Data = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"四格日程.xml");
    readonly XmlSerializer serializer = new XmlSerializer(typeof(List<Note>));
    List<Note> notes;
    Label summary;
    TableLayoutPanel matrix;
    static Label LabelOf(string s, float size, bool bold) {
        return new Label {Text=s,AutoSize=true,ForeColor=Ink,Font=new Font("Microsoft YaHei UI",size,bold?FontStyle.Bold:FontStyle.Regular)};
    }
    static Button ButtonOf(string s) {
        var b=new Button {Text=s,AutoSize=true,FlatStyle=FlatStyle.Flat,ForeColor=Ink,BackColor=Color.Transparent,Cursor=Cursors.Hand,TabStop=true};
        b.FlatAppearance.BorderSize=0;
        return b;
    }
    public Board() {
        notes=File.Exists(Data)?Read(Data):new List<Note>();
        Text="四格 · 桌面便利贴"; Font=new Font("Microsoft YaHei UI",10); BackColor=Color.FromArgb(250,249,244);
        StartPosition=FormStartPosition.CenterScreen; MinimumSize=new Size(610,500); ClientSize=new Size(790,570);
        var layout=new TableLayoutPanel {Dock=DockStyle.Fill,Padding=new Padding(19,10,19,15),ColumnCount=1,RowCount=2};
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute,83));layout.RowStyles.Add(new RowStyle(SizeType.Percent,100)); Controls.Add(layout);
        var head=new Panel {Dock=DockStyle.Fill};layout.Controls.Add(head,0,0);
        var title=LabelOf("四格日程",22,true);title.Location=new Point(1,0);head.Controls.Add(title);
        summary=LabelOf("",10,false);summary.Location=new Point(5,48);head.Controls.Add(summary);
        var pin=new CheckBox {Text="窗口置顶",Width=94,Dock=DockStyle.Right};
        head.Controls.Add(pin);
        pin.CheckedChanged+=delegate {TopMost=pin.Checked;};
        matrix=new TableLayoutPanel {Dock=DockStyle.Fill,RowCount=2,ColumnCount=2,Margin=Padding.Empty};
        for(int i=0;i<2;i++){matrix.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));matrix.RowStyles.Add(new RowStyle(SizeType.Percent,50));}
        layout.Controls.Add(matrix,0,1); Render();
        Activated+=delegate {UpdateSummary();};
    }
    List<Note> Read(string path) {using(var f=File.OpenRead(path)) return (List<Note>)serializer.Deserialize(f);}
    void Commit(List<Note> next) {
        string temp=Data+".tmp";
        try {
            using(var f=File.Create(temp)) serializer.Serialize(f,next);
            if(File.Exists(Data)) File.Replace(temp,Data,null);else File.Move(temp,Data);
            notes=next;
        } catch(Exception e){MessageBox.Show(this,"保存失败："+e.Message,Text);}
        Render();
    }
    void UpdateSummary(){summary.Text=DateTime.Today.ToString("M 月 d 日  dddd",new System.Globalization.CultureInfo("zh-CN"))+"    /    "+notes.Count(n=>!n.Done)+" 项待办";}
    void Render() {
        UpdateSummary();matrix.SuspendLayout();
        while(matrix.Controls.Count>0) matrix.Controls[0].Dispose();
        for(int i=0;i<4;i++){
            int group=i; var items=notes.Where(n=>n.Group==group).OrderBy(n=>n.Done).ThenBy(n=>n.Due??DateTime.MaxValue).ToList();
            var card=new Panel {Dock=DockStyle.Fill,BackColor=ColorTranslator.FromHtml(Colors[i]),Padding=new Padding(13),Margin=new Padding(5)};
            matrix.Controls.Add(card,i%2,i/2);
            var list=new Panel {Dock=DockStyle.Fill,AutoScroll=true};card.Controls.Add(list);
            var top=new Panel {Dock=DockStyle.Top,Height=42};card.Controls.Add(top);
            var heading=LabelOf(Names[i],14,true);top.Controls.Add(heading);
            var count=LabelOf(items.Count(n=>!n.Done).ToString("00"),11,false);count.Dock=DockStyle.Right;top.Controls.Add(count);
            var add=ButtonOf("＋ 添加日程");add.Dock=DockStyle.Bottom;add.TextAlign=ContentAlignment.MiddleLeft;card.Controls.Add(add);
            add.Click+=delegate {Edit(group,null);};
            if(items.Count==0){var empty=LabelOf("暂无日程",10,false);empty.ForeColor=Color.DimGray;empty.Location=new Point(2,9);list.Controls.Add(empty);}
            foreach(var n in Enumerable.Reverse(items)){
                var row=new Panel {Height=n.Due.HasValue?55:39,Dock=DockStyle.Top};list.Controls.Add(row);row.BringToFront();
                var box=new CheckBox {Checked=n.Done,Width=23,Dock=DockStyle.Left,AccessibleName="完成 "+n.Title};row.Controls.Add(box);
                var edit=ButtonOf(n.Title);edit.AutoSize=false;edit.AutoEllipsis=true;edit.TextAlign=ContentAlignment.MiddleLeft;
                if(n.Done) edit.Font=new Font(Font,FontStyle.Strikeout);
                edit.Anchor=AnchorStyles.Left|AnchorStyles.Top|AnchorStyles.Right;edit.SetBounds(24,0,row.ClientSize.Width-25,34);row.Controls.Add(edit);
                edit.Click+=delegate {Edit(group,n);};
                if(n.Due.HasValue){var date=LabelOf(n.Due.Value.ToString("yyyy/MM/dd  HH:mm"),9,false);date.Location=new Point(29,32);row.Controls.Add(date);}
                box.CheckedChanged+=delegate {var next=new List<Note>(notes);next[next.IndexOf(n)]=new Note{Title=n.Title,Group=n.Group,Due=n.Due,Done=box.Checked};Commit(next);};
            }
        }
        matrix.ResumeLayout();
    }
    void Edit(int group,Note old) {
        using(var f=new Form {Text=old==null?"添加日程":"编辑日程",ClientSize=new Size(374,245),StartPosition=FormStartPosition.CenterParent,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false,Font=Font,BackColor=BackColor}){
            var caption=LabelOf("日程内容",10,true);caption.Location=new Point(18,16);f.Controls.Add(caption);
            var title=new TextBox{Text=old==null?"":old.Title,MaxLength=300};title.SetBounds(20,45,334,30);f.Controls.Add(title);
            var name=LabelOf("所属象限",10,false);name.Location=new Point(18,93);f.Controls.Add(name);
            var groups=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList};groups.Items.AddRange(Names);groups.SelectedIndex=group;groups.SetBounds(110,88,244,30);f.Controls.Add(groups);
            var timed=new CheckBox{Text="安排时间",AutoSize=true,Checked=old!=null && old.Due.HasValue};timed.Location=new Point(20,141);f.Controls.Add(timed);
            var due=new DateTimePicker{Format=DateTimePickerFormat.Custom,CustomFormat="yyyy-MM-dd HH:mm",Value=timed.Checked?old.Due.Value:DateTime.Now,Enabled=timed.Checked};due.SetBounds(128,137,226,30);f.Controls.Add(due);
            timed.CheckedChanged+=delegate {due.Enabled=timed.Checked;};
            var save=ButtonOf("保存");save.BackColor=Color.FromArgb(217,229,208);save.SetBounds(272,192,82,34);f.Controls.Add(save);f.AcceptButton=save;
            save.Click+=delegate {if(title.Text.Trim().Length==0){title.Focus();return;}f.DialogResult=DialogResult.OK;};
            if(old!=null){var del=ButtonOf("删除");del.Location=new Point(15,192);f.Controls.Add(del);del.Click+=delegate {if(MessageBox.Show(f,"删除这条日程？","删除日程",MessageBoxButtons.YesNo)==DialogResult.Yes)f.DialogResult=DialogResult.Abort;};}
            var result=f.ShowDialog(this);var next=new List<Note>(notes);
            if(result==DialogResult.Abort) next.Remove(old);
            else if(result==DialogResult.OK){var n=new Note{Title=title.Text.Trim(),Group=groups.SelectedIndex,Done=old!=null && old.Done,Due=timed.Checked?(DateTime?)due.Value:null};if(old==null)next.Add(n);else next[next.IndexOf(old)]=n;}
            else return;
            Commit(next);
        }
    }
    [STAThread] static void Main(string[] args){
        Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
        bool first;using(var lockFile=new Mutex(true,"Local\\FourGrid_"+AppDomain.CurrentDomain.BaseDirectory.GetHashCode(),out first)){
            if(!first){MessageBox.Show("四格日程已经打开，请在任务栏中切换到便利贴。");return;}
            try {using(var board=new Board()){
                if(args.Length==2 && args[0]=="--preview"){
                    board.notes=new List<Note>{new Note{Group=0,Title="提交项目初稿",Due=DateTime.Today.AddDays(1).AddHours(10)},new Note{Group=1,Title="安排下周的学习计划"},new Note{Group=2,Title="确认会议时间"}};
                    board.Render();board.Show();Application.DoEvents();using(var bmp=new Bitmap(board.Width,board.Height)){board.DrawToBitmap(bmp,new Rectangle(Point.Empty,bmp.Size));bmp.Save(args[1],System.Drawing.Imaging.ImageFormat.Png);}
                } else Application.Run(board);
            }}catch(Exception e){MessageBox.Show("无法打开便利贴："+e.Message,"四格日程");}
        }
    }
}
