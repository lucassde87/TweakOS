using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
namespace TweakOS.Services;
public sealed class ReleaseInfo { public string TagName{get;set;}=""; public string Name{get;set;}=""; public string HtmlUrl{get;set;}=""; public string Body{get;set;}=""; public string DownloadUrl{get;set;}=""; }
public sealed class ReleaseService {
 public const string CurrentVersion="5.0.0";
 const string ApiUrl="https://api.github.com/repos/lucassde87/tweak/releases/latest";
 public async Task<ReleaseInfo?> GetLatestAsync(){
  using var c=new HttpClient{Timeout=TimeSpan.FromSeconds(10)}; c.DefaultRequestHeaders.UserAgent.ParseAdd("TweakOS/5.0");
  using var d=JsonDocument.Parse(await c.GetStringAsync(ApiUrl)); var r=d.RootElement; var x=new ReleaseInfo{
   TagName=r.TryGetProperty("tag_name",out var t)?t.GetString()??"":"",
   Name=r.TryGetProperty("name",out var n)?n.GetString()??"":"",
   HtmlUrl=r.TryGetProperty("html_url",out var h)?h.GetString()??"":"",
   Body=r.TryGetProperty("body",out var b)?b.GetString()??"":""
  };
  if(r.TryGetProperty("assets",out var a)) foreach(var z in a.EnumerateArray()){
   var name=z.TryGetProperty("name",out var nn)?nn.GetString()??"":"";
   if(name.EndsWith(".zip",StringComparison.OrdinalIgnoreCase)||name.EndsWith(".exe",StringComparison.OrdinalIgnoreCase)){x.DownloadUrl=z.GetProperty("browser_download_url").GetString()??"";break;}
  } return x;
 }
 public static bool IsNewer(string tag)=>Version.TryParse(tag.Trim().TrimStart('v','V'),out var r)&&Version.TryParse(CurrentVersion,out var c)&&r>c;
 public static void OpenUrl(string url){if(!string.IsNullOrWhiteSpace(url))Process.Start(new ProcessStartInfo{FileName=url,UseShellExecute=true});}
}