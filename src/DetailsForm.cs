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
  private readonly Label status = new Label(), updated = new Label(), notice = new Label();
  private readonly Button refresh = new Button(), close = new Button(), languageChoice = new Button();
  private readonly UiDarkChoice windowChoice = new UiDarkChoice();
  private readonly ToolTip tip = new ToolTip();
  private readonly List<QuotaWindow> windows = new List<QuotaWindow>();
  private bool binding, quitting, hasNotice, lastStale, lastBusy;
  private QuotaSnapshot lastSnapshot;
  private string lastSelectedId, lastMessage, lastSettingsMessage;
  public event EventHandler RefreshRequested;
  public event EventHandler SettingsChanged;
  public string SelectedLanguage { get { return UiText.Language; } }
  public string SelectedWindowId { get { return windowChoice.SelectedIndex >= 0 && windowChoice.SelectedIndex < windows.Count ? windows[windowChoice.SelectedIndex].Id : null; } }
  private float CardsHeight { get { return Math.Max(70, windows.Count * 74 + Math.Max(0, windows.Count - 1) * 8); } }
  private float ChoiceY { get { return 84 + CardsHeight + 14; } }
  private float FooterY { get { return ChoiceY + 46 + (hasNotice ? 38 : 0); } }
  private float LogicalHeight { get { return FooterY + 44; } }
  public DetailsForm(AppSettings settings)
  {
   UiText.Language = settings == null ? "zh" : settings.Language;
   FormBorderStyle=FormBorderStyle.None;StartPosition=FormStartPosition.Manual;ShowInTaskbar=false;
   AutoScaleMode=AutoScaleMode.None;BackColor=Theme.Background;DoubleBuffered=true;KeyPreview=true;
   cards.AutoScroll=false;cards.BackColor=Theme.Background;
   SetupButton(close);close.Text="×";close.Click+=delegate{Hide();};
   SetupButton(refresh);refresh.Click+=delegate{if(RefreshRequested!=null)RefreshRequested(this,EventArgs.Empty);};
   SetupButton(languageChoice);languageChoice.Click+=delegate{
    UiText.Language=UiText.English?"zh":"en";
    SetState(lastSnapshot,SelectedWindowId??lastSelectedId,lastStale,lastBusy,lastMessage,lastSettingsMessage);
    if(SettingsChanged!=null)SettingsChanged(this,EventArgs.Empty);
   };
   status.ForeColor=Theme.Muted;updated.ForeColor=Theme.Muted;notice.ForeColor=Theme.Warning;
   status.TextAlign=ContentAlignment.MiddleLeft;updated.TextAlign=ContentAlignment.MiddleLeft;
   windowChoice.BackColor=Theme.Card;windowChoice.ForeColor=Theme.Text;
   Controls.AddRange(new Control[]{cards,close,refresh,status,updated,notice,languageChoice,windowChoice});
   windowChoice.SelectedIndexChanged+=delegate{if(!binding&&SettingsChanged!=null)SettingsChanged(this,EventArgs.Empty);};
   ApplyScale(100);SetState(null,null,false,false,null,null);
  }
  private static void SetupButton(Button button){button.FlatStyle=FlatStyle.Flat;button.FlatAppearance.BorderColor=Theme.Border;button.FlatAppearance.MouseOverBackColor=Theme.Border;button.BackColor=Theme.Card;button.ForeColor=Theme.Text;button.Cursor=Cursors.Hand;}
  public void ApplyScale(int ignoredLegacyPercent){using(Graphics g=CreateGraphics())preferredScale=g.DpiX/96f;FitToWorkingArea(Screen.FromRectangle(Bounds).WorkingArea);}
  private void FitToWorkingArea(Rectangle area)
  {
   windowChoice.CloseDropDown();
   scale=Math.Min(preferredScale,Math.Min(Math.Max(1,area.Width-12)/360f,Math.Max(1,area.Height-12)/LogicalHeight));
   ClientSize=new Size(Math.Max(1,(int)Math.Floor(360*scale)),Math.Max(1,(int)Math.Floor(LogicalHeight*scale)));
   foreach(Control control in Controls){Font previous=control.Font;Font next=new Font("Microsoft YaHei UI",(control==status||control==updated?10:11)*scale,FontStyle.Regular,GraphicsUnit.Pixel);if(next.Equals(previous))next.Dispose();else{control.Font=next;if(previous!=Font&&previous!=SystemFonts.DefaultFont)previous.Dispose();}}
   windowChoice.ItemHeight=(int)(21*scale);LayoutControls();Invalidate();
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
   Box(languageChoice,20,ChoiceY,76,28);Box(windowChoice,108,ChoiceY,232,28);
   Box(notice,20,ChoiceY+38,320,34);
   Box(status,20,FooterY,124,28);Box(updated,148,FooterY,112,28);Box(refresh,268,FooterY,72,28);
   for(int i=0;i<cards.Controls.Count;i++){UiQuotaCard card=(UiQuotaCard)cards.Controls[i];card.ScaleFactor=scale;card.Bounds=Rectangle.Round(new RectangleF(0,i*82*scale,320*scale,74*scale));}
  }
  public void SetState(QuotaSnapshot snapshot,string selectedId,bool stale,bool busy,string message,string settingsMessage)
  {
   lastSnapshot=snapshot;lastSelectedId=selectedId;lastStale=stale;lastBusy=busy;lastMessage=message;lastSettingsMessage=settingsMessage;
   plan=snapshot==null||String.IsNullOrWhiteSpace(snapshot.PlanLabel)?UiText.T("套餐待获取","Plan unavailable"):UiText.Plan(snapshot.PlanLabel);
   Text=UiText.T("CodexUsage · 详情","CodexUsage");AccessibleName=UiText.T("CodexUsage详情和显示窗口选择","CodexUsage details and widget selection");
   cards.AccessibleName=UiText.T("全部额度窗口","All usage windows");close.AccessibleName=UiText.T("关闭详情","Close details");
   languageChoice.Text=UiText.T("English","中文");languageChoice.AccessibleName=UiText.T("切换为英文","Switch to Chinese");
   windowChoice.AccessibleName=UiText.T("浮条展示的额度窗口","Usage window shown in the widget");tip.SetToolTip(windowChoice,windowChoice.AccessibleName);
   status.AccessibleName=UiText.T("自动刷新频率","Automatic refresh interval");updated.AccessibleName=UiText.T("上次更新时间","Last successful update");refresh.AccessibleName=UiText.T("立即刷新套餐和额度","Refresh plan and usage");notice.AccessibleName=UiText.T("读取状态","Connection status");
   binding=true;
   string oldIds=String.Join("|",windows.ConvertAll(delegate(QuotaWindow w){return w.Id;}).ToArray());
   List<QuotaWindow> next=snapshot==null||snapshot.Windows==null?new List<QuotaWindow>():snapshot.Windows;
   string newIds=String.Join("|",next.ConvertAll(delegate(QuotaWindow w){return w.Id;}).ToArray());
   windows.Clear();windows.AddRange(next);
   if(oldIds!=newIds||cards.Controls.Count!=windows.Count){while(cards.Controls.Count>0){Control child=cards.Controls[0];cards.Controls.Remove(child);child.Dispose();}windowChoice.Items.Clear();foreach(QuotaWindow window in windows){cards.Controls.Add(new UiQuotaCard());windowChoice.Items.Add(UiText.WindowLabel(window.Label));}}
   for(int i=0;i<windows.Count;i++){string label=UiText.WindowLabel(windows[i].Label);if(!String.Equals(Convert.ToString(windowChoice.Items[i]),label,StringComparison.Ordinal))windowChoice.Items[i]=label;}
   windowChoice.SelectedIndex=windows.FindIndex(delegate(QuotaWindow w){return w.Id==selectedId;});windowChoice.Enabled=windows.Count>0;
   for(int i=0;i<windows.Count;i++){((UiQuotaCard)cards.Controls[i]).SetState(windows[i],stale,windows[i].Id==selectedId);tip.SetToolTip(cards.Controls[i],UiText.WindowLabel(windows[i].Label)+"\n"+Theme.ResetText(windows[i],DateTimeOffset.UtcNow)+(windows[i].ResetsAtUtc.HasValue?"\n"+windows[i].ResetsAtUtc.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss zzz"):""));} if(windowChoice.SelectedIndex>=0)tip.SetToolTip(windowChoice,windowChoice.AccessibleName+"\n"+UiText.WindowLabel(windows[windowChoice.SelectedIndex].Label));
   binding=false;refresh.Enabled=!busy;refresh.Text=busy?UiText.T("刷新中","Loading"):UiText.T("立即刷新","Refresh");
   status.Text=UiText.T("额度每5分钟自动刷新","Every 5 min");
   updated.Text=snapshot==null?UiText.T("尚未更新","Not updated"):UiText.T("更新于 ","Updated ")+snapshot.FetchedAtUtc.ToLocalTime().ToString("HH:mm:ss");
   tip.SetToolTip(updated,snapshot==null?updated.Text:UiText.T("完整更新时间：","Last updated: ")+snapshot.FetchedAtUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss zzz"));
   string text=busy?UiText.T("正在读取 Codex 账号套餐与额度…","Reading your Codex plan and usage…"):!String.IsNullOrEmpty(message)?UiText.Error(message):snapshot==null?UiText.T("等待获取额度。请先在 Codex 中登录。","Waiting for usage. Sign in to Codex first."):stale?UiText.T("上次结果已过期，请刷新后查看。","The previous result is out of date. Please refresh."):String.Empty;
   if(!String.IsNullOrEmpty(settingsMessage))text=UiText.Error(settingsMessage)+" "+text;
   hasNotice=!String.IsNullOrEmpty(text);notice.Text=text;notice.Visible=hasNotice;notice.ForeColor=busy&&String.IsNullOrEmpty(settingsMessage)?Theme.Muted:Theme.Warning;
   cards.AccessibleDescription=UiText.T("额度窗口数量：","Usage windows: ")+windows.Count;
   FitToWorkingArea(Screen.FromRectangle(Bounds).WorkingArea);Bounds=Theme.Clamp(Bounds,Screen.FromRectangle(Bounds).WorkingArea);Invalidate();
  }
  public void ShowAnchored(WidgetForm widget){Rectangle area=Screen.FromControl(widget).WorkingArea;FitToWorkingArea(area);int gap=(int)(10*scale),x=widget.Right-Width,y=widget.Top-Height-gap;if(y<area.Top)y=widget.Bottom+gap;Bounds=Theme.Clamp(new Rectangle(x,y,Width,Height),area);Show();Activate();}
  protected override void OnPaint(PaintEventArgs e)
  {
   base.OnPaint(e);Graphics g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;
   Theme.Rounded(g,new RectangleF(.5f,.5f,Width-1,Height-1),18*scale,Theme.Background,Theme.Border);
   Theme.Write(g,UiText.AppName,21,18,275,28,22,Theme.Text,true,scale);Theme.Write(g,UiText.T("账号套餐","Account plan"),22,52,83,17,10,Theme.Muted,false,scale);
   Theme.Rounded(g,new RectangleF(108*scale,51*scale,180*scale,21*scale),7*scale,Theme.Card,null);Theme.Write(g,plan,117,51,163,21,11,Theme.Aqua,true,scale);
   using(Pen p=new Pen(Theme.Border))g.DrawLine(p,20*scale,(FooterY-8)*scale,340*scale,(FooterY-8)*scale);
   if(cards.Controls.Count==0)Theme.Write(g,UiText.T("额度信息将在读取成功后显示","Usage appears after a successful refresh"),30,94,300,42,11,Theme.Muted,false,scale);
  }
  public void Shutdown(){quitting=true;Close();}
  protected override void OnVisibleChanged(EventArgs e){if(!Visible&&windowChoice!=null)windowChoice.CloseDropDown();base.OnVisibleChanged(e);}
  protected override void OnFormClosing(FormClosingEventArgs e){if(!quitting&&e.CloseReason==CloseReason.UserClosing){e.Cancel=true;Hide();}base.OnFormClosing(e);}
  protected override void OnKeyDown(KeyEventArgs e){base.OnKeyDown(e);if(e.KeyCode==Keys.Escape){Hide();e.Handled=true;}}
  protected override void Dispose(bool disposing){if(disposing)tip.Dispose();base.Dispose(disposing);}
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
   if(!pending&&window.RemainingPercent.HasValue&&window.RemainingPercent.Value>0){track.Width*=(float)(window.RemainingPercent.Value/100);Theme.Rounded(g,track,2.5f*s,color,null);}
   string countdown=stale?UiText.T("上次结果 · 已过期","Previous result · Out of date"):pending?UiText.T("已重置 · 待更新","Reset reached · Pending"):Theme.ResetText(window,now);
   Theme.Write(g,countdown,12,49,width-150,17,9.5f,stale||pending?Theme.Warning:Theme.Blue,false,s);
   string reset=window.ResetsAtUtc.HasValue?window.ResetsAtUtc.Value.ToLocalTime().ToString("MM-dd HH:mm"):UiText.T("重置时间未知","Reset unknown");
   using(Font font=new Font("Microsoft YaHei UI",9.5f*s,FontStyle.Regular,GraphicsUnit.Pixel))TextRenderer.DrawText(g,reset,font,Rectangle.Round(new RectangleF((width-136)*s,49*s,124*s,17*s)),Theme.Muted,TextFormatFlags.Right|TextFormatFlags.VerticalCenter|TextFormatFlags.SingleLine|TextFormatFlags.NoPadding);
  }
 }
}


