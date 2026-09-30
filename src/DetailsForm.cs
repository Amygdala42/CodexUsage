using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
namespace CodexQuotaLite
{
 public sealed class DetailsForm : Form
 {
  private float scale, preferredScale;
  private string plan;
  private readonly Panel cards = new Panel();
  private readonly Label status = new Label(), updated = new Label();
  private readonly LinkLabel github = new LinkLabel();
  private readonly Label resetNotice = new Label();
  private readonly LinkLabel resetSource = new LinkLabel();
  private ResetFeed resetFeed;
  private readonly Label version = new Label();
  private readonly Button refresh = new Button(), close = new Button(), languageChoice = new Button(), themeChoice = new Button();
  private readonly UiDarkChoice windowChoice = new UiDarkChoice();
  private readonly List<QuotaWindow> windows = new List<QuotaWindow>();
  private bool binding, quitting, lastStale, lastBusy;
  private WidgetForm anchor;
  private QuotaSnapshot lastSnapshot;
  private string lastSelectedId, lastMessage, lastSettingsMessage, themeMode;
  public event EventHandler RefreshRequested;
  public event EventHandler SettingsChanged;
  public string SelectedLanguage { get { return UiText.Language; } }
  public string SelectedThemeMode { get { return themeMode; } }
  public string SelectedWindowId { get { return windowChoice.SelectedIndex >= 0 && windowChoice.SelectedIndex < windows.Count ? windows[windowChoice.SelectedIndex].Id : null; } }
  private float CardsHeight { get { return DetailsLayout.CardsHeight(windows.Count); } }
  private float FooterY { get { return DetailsLayout.FooterY(windows.Count); } }
  private float LogicalHeight { get { return DetailsLayout.LogicalHeight(windows.Count); } }
  public DetailsForm(AppSettings settings)
  {
   UiText.Language = settings == null ? "zh" : settings.Language;
   Theme.Apply(settings == null ? "dark" : settings.ThemeMode);themeMode=Theme.Mode;
   FormBorderStyle=FormBorderStyle.None;StartPosition=FormStartPosition.Manual;ShowInTaskbar=false;
   AutoScaleMode=AutoScaleMode.None;BackColor=Theme.Background;DoubleBuffered=true;KeyPreview=true;
   cards.AutoScroll=false;cards.BackColor=Theme.Background;
   resetNotice.ForeColor=Theme.Muted;
   resetSource.LinkColor=Theme.Blue;resetSource.ActiveLinkColor=Theme.Aqua;resetSource.VisitedLinkColor=Theme.Blue;
   resetSource.UseCompatibleTextRendering=false;resetSource.TextAlign=ContentAlignment.MiddleRight;
   resetNotice.UseCompatibleTextRendering=false;resetNotice.TextAlign=ContentAlignment.MiddleLeft;
   resetSource.LinkBehavior=LinkBehavior.HoverUnderline;
   resetSource.LinkClicked+=delegate{
    string url=resetFeed!=null&&resetFeed.Latest!=null?resetFeed.Latest.Url:"https://codex-resets.com/";
    try{System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url){UseShellExecute=true});}
    catch(System.ComponentModel.Win32Exception){resetNotice.Text=UiText.T("无法打开浏览器","Cannot open browser");}
    catch(InvalidOperationException){resetNotice.Text=UiText.T("无法打开浏览器","Cannot open browser");}
   };
   github.Text=UiText.T("GITHUB主页","GITHUB");github.LinkColor=Theme.Blue;github.ActiveLinkColor=Theme.Aqua;
   github.VisitedLinkColor=Theme.Blue;github.LinkBehavior=LinkBehavior.HoverUnderline;
   github.TextAlign=ContentAlignment.MiddleCenter;github.Cursor=Cursors.Hand;
   github.UseCompatibleTextRendering=false;
   github.LinkClicked+=delegate{
    try{System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://github.com/Amygdala42/CodexUsage"){UseShellExecute=true});}
    catch(System.ComponentModel.Win32Exception){ShowLinkError();}
    catch(InvalidOperationException){ShowLinkError();}
   };
   version.Text="v"+typeof(DetailsForm).Assembly.GetName().Version.ToString(3);
   version.ForeColor=Theme.Muted;version.TextAlign=ContentAlignment.MiddleCenter;
   version.UseCompatibleTextRendering=false;
   SetupButton(close);close.Text="×";close.Click+=delegate{Hide();};
   SetupButton(refresh);refresh.Click+=delegate{if(RefreshRequested!=null)RefreshRequested(this,EventArgs.Empty);};
   SetupButton(languageChoice);languageChoice.Click+=delegate{
    UiText.Language=UiText.English?"zh":"en";
    SetState(lastSnapshot,SelectedWindowId??lastSelectedId,lastStale,lastBusy,lastMessage,lastSettingsMessage);
    if(SettingsChanged!=null)SettingsChanged(this,EventArgs.Empty);
   };
   SetupButton(themeChoice);themeChoice.Click+=delegate{
    SetTheme(themeMode=="dark"?"light":"dark");
    if(SettingsChanged!=null)SettingsChanged(this,EventArgs.Empty);
   };
   status.ForeColor=Theme.Muted;updated.ForeColor=Theme.Muted;status.AutoEllipsis=false;
   status.TextAlign=ContentAlignment.MiddleLeft;updated.TextAlign=ContentAlignment.MiddleLeft;
   windowChoice.BackColor=Theme.Card;windowChoice.ForeColor=Theme.Text;
   Controls.AddRange(new Control[]{cards,close,refresh,status,updated,languageChoice,themeChoice,windowChoice,github,version,resetNotice,resetSource});
   windowChoice.SelectedIndexChanged+=delegate{if(!binding&&SettingsChanged!=null)SettingsChanged(this,EventArgs.Empty);};
   ApplyScale(100);SetState(null,null,false,false,null,null);
  }
  private static void SetupButton(Button button){button.FlatStyle=FlatStyle.Flat;button.FlatAppearance.BorderColor=Theme.Border;button.FlatAppearance.MouseOverBackColor=Theme.Border;button.FlatAppearance.MouseDownBackColor=Theme.Border;button.BackColor=Theme.Card;button.ForeColor=Theme.Text;button.Cursor=Cursors.Hand;}
  internal void SetTheme(string mode)
  {
   Theme.Apply(mode);themeMode=Theme.Mode;
   BackColor=Theme.Background;cards.BackColor=Theme.Background;
   foreach(Control control in Controls){control.BackColor=Theme.Background;control.ForeColor=Theme.Text;}
   foreach(Button button in new[]{close,refresh,languageChoice,themeChoice})SetupButton(button);
   foreach(LinkLabel link in new[]{github,resetSource}){link.LinkColor=Theme.Blue;link.ActiveLinkColor=Theme.Aqua;link.VisitedLinkColor=Theme.Blue;}
   resetNotice.ForeColor=Theme.Muted;version.ForeColor=Theme.Muted;updated.ForeColor=Theme.Muted;
   foreach(Control card in cards.Controls){card.BackColor=Theme.Background;card.Invalidate();}
   windowChoice.ApplyTheme();
   SetState(lastSnapshot,SelectedWindowId??lastSelectedId,lastStale,lastBusy,lastMessage,lastSettingsMessage);
   Invalidate(true);
  }
  private void ShowLinkError(){MessageBox.Show(this,UiText.T("无法打开浏览器。项目主页：https://github.com/Amygdala42/CodexUsage","Could not open your browser. Project page: https://github.com/Amygdala42/CodexUsage"),UiText.AppName,MessageBoxButtons.OK,MessageBoxIcon.Information);}
  public void ApplyScale(int ignoredLegacyPercent){using(Graphics g=CreateGraphics())preferredScale=g.DpiX/96f;FitToWorkingArea(Screen.FromRectangle(Bounds).WorkingArea);}
  private void FitToWorkingArea(Rectangle area)
  {
   float nextScale=Math.Min(preferredScale,Math.Min(Math.Max(1,area.Width-12)/360f,Math.Max(1,area.Height-12)/LogicalHeight));
   Size nextSize=new Size(Math.Max(1,(int)Math.Floor(360*nextScale)),Math.Max(1,(int)Math.Floor(LogicalHeight*nextScale)));
   // A clock/usage repaint must not dismiss a choice that the user is making.
   if(scale!=nextScale||ClientSize!=nextSize)windowChoice.CloseDropDown();
   scale=nextScale;ClientSize=nextSize;
   foreach(Control control in Controls){Font previous=control.Font;Font next=new Font(DetailsLayout.ControlFontFamily,(control==github||control==version?12:control==status||control==updated?10:DetailsLayout.ControlFontSize)*scale,FontStyle.Regular,GraphicsUnit.Pixel);if(next.Equals(previous))next.Dispose();else{control.Font=next;if(previous!=Font&&previous!=SystemFonts.DefaultFont)previous.Dispose();}}
   windowChoice.ItemHeight=DetailsLayout.ChoiceItemHeight(scale);LayoutControls();Invalidate();
  }
  protected override void OnSizeChanged(EventArgs e)
  {
   base.OnSizeChanged(e);if(scale<=0||Width<=0||Height<=0)return;
   using(GraphicsPath path=Theme.Round(new RectangleF(0,0,Width,Height),18*scale)){Region old=Region;Region=new Region(path);if(old!=null)old.Dispose();}
   LayoutControls();Invalidate();
  }
  private void Box(Control control,float x,float y,float width,float height){control.Bounds=Rectangle.Round(new RectangleF(x*scale,y*scale,width*scale,height*scale));}
  private void LayoutControls()
  {
   Box(close,309,17,31,29);Box(cards,20,84,320,CardsHeight);
   Box(version,176,18,43,28);Box(github,222,18,82,28);
   Box(resetNotice,20,84+CardsHeight+10,262,28);
   Box(resetSource,290,84+CardsHeight+10,50,28);
   DetailsLayout.Row choiceRow=DetailsLayout.ChoiceRow(windows.Count,scale);
   languageChoice.Bounds=choiceRow.Language;themeChoice.Bounds=choiceRow.Theme;windowChoice.Bounds=choiceRow.Selector;
   Box(status,20,FooterY,124,28);Box(updated,148,FooterY,112,28);Box(refresh,268,FooterY,72,28);
   for(int i=0;i<cards.Controls.Count;i++){UiQuotaCard card=(UiQuotaCard)cards.Controls[i];card.ScaleFactor=scale;card.Bounds=Rectangle.Round(new RectangleF(0,i*82*scale,320*scale,74*scale));}
  }
  public void SetState(QuotaSnapshot snapshot,string selectedId,bool stale,bool busy,string message,string settingsMessage)
  {
   UpdateResetNotice();
   lastSnapshot=snapshot;lastSelectedId=selectedId;lastStale=stale;lastBusy=busy;lastMessage=message;lastSettingsMessage=settingsMessage;
   plan=snapshot==null||String.IsNullOrWhiteSpace(snapshot.PlanLabel)?UiText.T("套餐待获取","Plan unavailable"):UiText.Plan(snapshot.PlanLabel);
   Text=UiText.T("CodexUsage · 详情","CodexUsage");AccessibleName=UiText.T("CodexUsage详情和显示窗口选择","CodexUsage details and widget selection");
   github.Text=UiText.T("GITHUB主页","GITHUB");github.AccessibleName=UiText.T("打开 GitHub 项目主页","Open the GitHub project page");
   version.AccessibleName=UiText.T("版本 ","Version ")+version.Text;
   cards.AccessibleName=UiText.T("全部额度窗口","All usage windows");close.AccessibleName=UiText.T("关闭详情","Close details");
   languageChoice.Text=DetailsLayout.LanguageCaption();languageChoice.AccessibleName=UiText.T("切换为英文","Switch to Chinese");
   themeChoice.Text=DetailsLayout.ThemeCaption(Theme.IsDark);
   themeChoice.AccessibleName=Theme.IsDark?UiText.T("切换到浅色模式","Switch to light mode"):UiText.T("切换到深色模式","Switch to dark mode");
   themeChoice.AccessibleDescription=Theme.IsDark?UiText.T("当前为深色模式","Currently using dark mode"):UiText.T("当前为浅色模式","Currently using light mode");
   windowChoice.AccessibleName=UiText.T("浮条展示的额度窗口","Usage window shown in the widget");
   status.AccessibleName=UiText.T("刷新状态","Refresh status");updated.AccessibleName=UiText.T("上次更新时间","Last successful update");refresh.AccessibleName=UiText.T("立即刷新套餐和额度","Refresh plan and usage");
   binding=true;
   string oldIds=String.Join("|",windows.ConvertAll(delegate(QuotaWindow w){return w.Id;}).ToArray());
   List<QuotaWindow> next=snapshot==null||snapshot.Windows==null?new List<QuotaWindow>():snapshot.Windows;
   string newIds=String.Join("|",next.ConvertAll(delegate(QuotaWindow w){return w.Id;}).ToArray());
   windows.Clear();windows.AddRange(next);
   if(oldIds!=newIds||cards.Controls.Count!=windows.Count){while(cards.Controls.Count>0){Control child=cards.Controls[0];cards.Controls.Remove(child);child.Dispose();}windowChoice.Items.Clear();foreach(QuotaWindow window in windows){cards.Controls.Add(new UiQuotaCard());windowChoice.Items.Add(UiText.WindowLabel(window.Label));}}
   for(int i=0;i<windows.Count;i++){string label=UiText.WindowLabel(windows[i].Label);if(!String.Equals(Convert.ToString(windowChoice.Items[i]),label,StringComparison.Ordinal))windowChoice.Items[i]=label;}
   windowChoice.SelectedIndex=windows.FindIndex(delegate(QuotaWindow w){return w.Id==selectedId;});windowChoice.Enabled=windows.Count>0;
   for(int i=0;i<windows.Count;i++){((UiQuotaCard)cards.Controls[i]).SetState(windows[i],stale,windows[i].Id==selectedId);}
   binding=false;refresh.Enabled=!busy;refresh.Text=busy?UiText.T("刷新中","Loading"):UiText.T("立即刷新","Refresh");
   status.Text=UiText.T("额度每5分钟自动刷新","Every 5 min");
   updated.Text=snapshot==null?UiText.T("尚未更新","Not updated"):UiText.T("更新于 ","Updated ")+snapshot.FetchedAtUtc.ToLocalTime().ToString("HH:mm:ss");
   string text=busy?UiText.T("正在读取 Codex 账号套餐与额度…","Reading your Codex plan and usage…"):!String.IsNullOrEmpty(message)?UiText.Error(message):snapshot==null?UiText.T("等待获取额度。请先在 Codex 中登录。","Waiting for usage. Sign in to Codex first."):stale?UiText.T("上次结果已过期，请刷新后查看。","The previous result is out of date. Please refresh."):String.Empty;
   if(!String.IsNullOrEmpty(settingsMessage))text=UiText.Error(settingsMessage)+" "+text;
   bool problem=!String.IsNullOrEmpty(message)||!String.IsNullOrEmpty(settingsMessage)||stale;
   status.Text=busy?UiText.T("正在刷新…","Refreshing…"):problem?UiText.T("刷新异常","Refresh issue"):snapshot==null?UiText.T("等待获取额度","Waiting for usage"):UiText.T("额度每5分钟自动刷新","Every 5 min");
   status.ForeColor=problem?Theme.Warning:Theme.Muted;status.AccessibleDescription=text;
   cards.AccessibleDescription=UiText.T("额度窗口数量：","Usage windows: ")+windows.Count;
   if(Visible&&anchor!=null&&!anchor.IsDisposed)RepositionAnchored();
   else{FitToWorkingArea(Screen.FromRectangle(Bounds).WorkingArea);Bounds=Theme.Clamp(Bounds,Screen.FromRectangle(Bounds).WorkingArea);}Invalidate();
  }
  internal void SetResetFeed(ResetFeed feed){resetFeed=feed;UpdateResetNotice();}
  private void UpdateResetNotice()
  {
   resetNotice.Text=resetFeed!=null&&resetFeed.Latest!=null?resetFeed.Latest.Caption(UiText.English,resetFeed.Cached):
    resetFeed!=null&&resetFeed.Failed?UiText.T("重置公告暂不可用","Reset news unavailable"):
    UiText.T("暂无重置公告","No reset news");
   resetSource.Text=UiText.T("来源","Source");
   resetSource.AccessibleName=UiText.T("查看重置公告来源","View reset announcement source");
   resetNotice.AccessibleDescription=UiText.T("来源 codex-resets.com，公共重置公告，不代表个人账号到账时间。","Source: codex-resets.com. Public announcement, not confirmation of an account reset.");
  }
  public void ShowAnchored(WidgetForm widget){anchor=widget;RepositionAnchored();Show();Activate();}
  private void RepositionAnchored(){Rectangle area=Screen.FromControl(anchor).WorkingArea;FitToWorkingArea(area);int gap=(int)(10*scale),x=anchor.Right-Width,y=anchor.Top-Height-gap;if(y<area.Top)y=anchor.Bottom+gap;Bounds=Theme.Clamp(new Rectangle(x,y,Width,Height),area);}
  internal bool ContainsPointer(Point point){return Visible&&(Bounds.Contains(point)||windowChoice.DropDownContains(point));}
  protected override void OnPaint(PaintEventArgs e)
  {
   base.OnPaint(e);Graphics g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;
   Theme.Rounded(g,new RectangleF(.5f,.5f,Width-1,Height-1),18*scale,Theme.Background,Theme.Border);
   Theme.Write(g,UiText.AppName,21,18,153,28,22,Theme.Text,true,scale);Theme.Write(g,UiText.T("账号套餐","Account plan"),22,52,83,17,10,Theme.Muted,false,scale);
   Theme.Rounded(g,new RectangleF(108*scale,51*scale,180*scale,21*scale),7*scale,Theme.Card,null);Theme.Write(g,plan,117,51,163,21,11,Theme.Aqua,true,scale);
   using(Pen p=new Pen(Theme.Border))g.DrawLine(p,20*scale,(FooterY-8)*scale,340*scale,(FooterY-8)*scale);
   if(cards.Controls.Count==0)Theme.Write(g,UiText.T("额度信息将在读取成功后显示","Usage appears after a successful refresh"),30,94,300,42,11,Theme.Muted,false,scale);
  }
  public void Shutdown(){quitting=true;Close();}
  protected override void OnVisibleChanged(EventArgs e){if(!Visible&&windowChoice!=null)windowChoice.CloseDropDown();base.OnVisibleChanged(e);}
  protected override void OnLocationChanged(EventArgs e){if(windowChoice!=null)windowChoice.CloseDropDown();base.OnLocationChanged(e);}
  protected override void OnFormClosing(FormClosingEventArgs e){if(!quitting&&e.CloseReason==CloseReason.UserClosing){e.Cancel=true;Hide();}base.OnFormClosing(e);}
  protected override void OnKeyDown(KeyEventArgs e){base.OnKeyDown(e);if(e.KeyCode==Keys.Escape){Hide();e.Handled=true;}}
 }
 internal sealed class UiQuotaCard : Control
 {
  private QuotaWindow window;private bool stale,selected;internal float ScaleFactor=1;
  internal UiQuotaCard(){DoubleBuffered=true;BackColor=Theme.Background;}
  internal void SetState(QuotaWindow value,bool expired,bool active){window=value;stale=expired;selected=active;DateTimeOffset now=DateTimeOffset.UtcNow;AccessibleName=UiText.WindowLabel(value.Label)+(value.IsResetPending(now)?UiText.T("，已到重置时间，待更新",", reset reached; awaiting update"):UiText.T("，剩余额度 ",", remaining ")+Theme.Percent(value.RemainingPercent)+", "+Theme.ResetText(value,now));if(stale)AccessibleName+=UiText.T("，上次结果已过期","; previous result is out of date");Invalidate();}
  protected override void OnPaint(PaintEventArgs e)
  {
   if(window==null)return;Graphics g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;float s=ScaleFactor,width=Width/s;DateTimeOffset now=DateTimeOffset.UtcNow;bool pending=window.IsResetPending(now);Color color=stale||pending?Theme.Muted:Theme.Aqua;
   Theme.Rounded(g,new RectangleF(.5f,.5f,Width-1,Height-1),11*s,Theme.Card,selected?Theme.Border:(Color?)null);
   Theme.Write(g,UiText.WindowLabel(window.Label),12,8,width-105,22,12,Theme.Text,true,s);Theme.Write(g,pending?UiText.T("待更新","Pending"):Theme.Percent(window.RemainingPercent),width-87,7,75,23,pending?13:19,color,true,s);
   RectangleF track=new RectangleF(12*s,37*s,(width-24)*s,5*s);Theme.Rounded(g,track,2.5f*s,Theme.Border,null);
   if(!pending&&window.RemainingPercent.HasValue&&window.RemainingPercent.Value>0){track.Width*=(float)(window.RemainingPercent.Value/100);Theme.Rounded(g,track,2.5f*s,stale?Theme.Muted:Theme.WidgetTimeColor,null);}
   string countdown=stale?UiText.T("上次结果 · 已过期","Previous result · Out of date"):pending?UiText.T("已重置 · 待更新","Reset reached · Pending"):Theme.ResetText(window,now);
   Theme.Write(g,countdown,12,49,width-150,17,9.5f,stale||pending?Theme.Warning:Theme.Blue,false,s);
   string reset=window.ResetsAtUtc.HasValue?window.ResetsAtUtc.Value.ToLocalTime().ToString("MM-dd HH:mm"):UiText.T("重置时间未知","Reset unknown");
   using(Font font=new Font("Microsoft YaHei UI",9.5f*s,FontStyle.Regular,GraphicsUnit.Pixel))TextRenderer.DrawText(g,reset,font,Rectangle.Round(new RectangleF((width-136)*s,49*s,124*s,17*s)),Theme.Muted,TextFormatFlags.Right|TextFormatFlags.VerticalCenter|TextFormatFlags.SingleLine|TextFormatFlags.NoPadding);
  }
 }
}


