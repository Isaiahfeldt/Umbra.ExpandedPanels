using ExpandedPanels;
int n=0;
void Check(bool ok) { if(!ok) throw new Exception($"Check {n+1} failed"); n++; }
var data=new PanelData();
for(int p=0;p<4;p++) for(int s=0;s<256;s++) data.Set(p,s,new("EM",(uint)(p*256+s)));
var copy=PanelData.Parse(data.Serialize());
for(int p=0;p<4;p++) for(int s=0;s<256;s++) Check(copy.Get(p,s)==data.Get(p,s));
// Layout resizing does not mutate stored assignments; hidden slots survive saves.
copy.Set(0,0,null); Check(copy.Get(0,0)==null); Check(copy.Get(0,255)!=null); Check(copy.Get(1,0)!=null);
Check(PanelData.Parse(copy.Serialize()).Get(3,255)==new Slot("EM",1023));
Check(PanelData.ParseClipboard("SC:EM/42")==new Slot("EM",42));
Check(PanelData.ParseClipboard("SC:IM/0")==new Slot("IM",0));
foreach(var invalid in new[]{"", "EM/42", "SC:EM/no", "SC:/42", "SC:EM/1/2"}) Check(PanelData.ParseClipboard(invalid)==null);
try { PanelData.Parse("{bad}"); throw new Exception("Accepted corrupt data"); } catch(System.Text.Json.JsonException) {n++;}
try { data.Set(4,0,new("EM",1)); throw new Exception("Accepted invalid page"); } catch(ArgumentOutOfRangeException) {n++;}
Check(PanelData.EnabledPages(new[]{"One", "", "Three", " "}).SequenceEqual(new[]{0,2}));
Check(PanelData.EnabledPages(new[]{"", " ", "", ""}).Length==0);
Check(PanelData.EnabledPages(new[]{"One", "Two", "Three", "Four"}).Length==4);
Check(PanelData.Parse(data.Serialize()).Get(1,42)==new Slot("EM",298));
Console.WriteLine($"Passed {n} data and clipboard checks.");
