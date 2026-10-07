using System.Net.Http;
using System.Text.Json;
using TweakOS.Models;
namespace TweakOS.Services;
public sealed class TweakCatalog{public int SchemaVersion{get;set;}public int CatalogVersion{get;set;}public string UpdatedAt{get;set;}="";public List<string> ReleaseNotes{get;set;}=[];public List<TweakDefinition> Tweaks{get;set;}=[];}
public sealed class TweakCatalogService{
 const string RemoteUrl="https://raw.githubusercontent.com/lucassde87/tweak/main/catalog/tweaks.json";
 readonly JsonSerializerOptions options=new(){PropertyNameCaseInsensitive=true};
 public async Task<TweakCatalog> LoadAsync(){
  using var c=new HttpClient{Timeout=TimeSpan.FromSeconds(10)};
  try{var r=JsonSerializer.Deserialize<TweakCatalog>(await c.GetStringAsync(RemoteUrl),options);if(r!=null)return r;}catch{}
  var local=Path.Combine(AppContext.BaseDirectory,"catalog","tweaks.json");
  if(!File.Exists(local))local=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"..","..","..","..","catalog","tweaks.json"));
  if(!File.Exists(local))throw new FileNotFoundException("Lokaler Tweak-Katalog nicht gefunden.",local);
  return JsonSerializer.Deserialize<TweakCatalog>(await File.ReadAllTextAsync(local),options)??throw new InvalidDataException("Tweak-Katalog ist ungültig.");
 }
}