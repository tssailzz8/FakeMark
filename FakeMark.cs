using System;
using System.Collections.Generic;
using System.Diagnostics;
using Num = System.Numerics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static ImGuiNET.ImGui;
using System.Linq;
using Dalamud.Game;
using Dalamud.Plugin;
using ImGuiNET;
using Dalamud.Hooking;
using StbiSharp;
using ImGuiScene;
using System.Numerics;
using Dalamud.Logging;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Interface;
using FakeMark.Struck;
using static FakeMark.Raid;
using Dalamud.Game.ClientState.Conditions;

namespace FakeMark
{
	public unsafe class FakeMark : IDalamudPlugin
	{
		public string Name => "FakeMark";
		//public static Configuration Config { get; private set; }
		private Configuration config;
		private DalamudPluginInterface pluginInterface;
		public unsafe delegate Int64 HeadCall(IntPtr a, uint b);
		private Hook<HeadCall> headCall;
		private static Dictionary<int, TextureWrap> _yellowNumberTextures = new();
		private static Dictionary<int, TextureWrap> _redNumberTextures = new();
		private static Dictionary<string, TextureWrap> _marksNumberTextures = new();
		private Dictionary<int, XivApi.SafeNamePlateObject> array;
		public static Dictionary<Dalamud.Game.ClientState.Objects.Types.GameObject, Marks> Marks = new();
		public static Dictionary<uint, TextureWrap?> Icon = new();
		public static string Stage;
		private delegate void CastDelegate(uint sourceId, IntPtr sourceCharacter);
		private delegate void ActorControlSelfDelegate(uint entityId, uint id, uint arg0, uint arg1, uint arg2,
													   uint arg3, uint arg4, uint arg5, ulong targetId, byte a10);
		public Log log;

		private Hook<ActorControlSelfDelegate> ActorControlSelfHook;
		public static DateTime createTime;
		private IntPtr 计算数;
		private Hook<CastDelegate> CastHook;
		public bool IsVisible { get; private set; }
		public int MaxTextureHeight { get; private set; }

		public FakeMark(DalamudPluginInterface pluginInterface)
		{
			this.pluginInterface = pluginInterface;
			DalamudApi.Initialize(this, pluginInterface);
			var address = new PluginAddressResolver();
			address.Setup(DalamudApi.SigScanner);
			XivApi.Initialize(pluginInterface, address);
			
			this.config = (((Configuration)this.pluginInterface.GetPluginConfig()) ?? new Configuration());
			this.config.Initialize();
			for (uint i = 62100; i < 62141; i++) Icon.Add(i - 62100, DalamudApi.GameData.GetImGuiTextureHqIcon(i));
			this.config.ConfigUiVisible = false;
			headCall = new Hook<HeadCall>(
					DalamudApi.SigScanner.ScanText("E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? 4D 85 F6 0F 84 ?? ?? ?? ?? 41 0F B6 D5 49 8B CE 45 85 E4"), headcall);
			headCall.Enable();
			计算数 = DalamudApi.SigScanner.GetStaticAddressFromSig("75 25 8B 0D ?? ?? ?? ??",2);
			var 计算数1 = Marshal.ReadInt32(计算数);
			var 计算数2 = Marshal.ReadInt32(计算数 + 4);
		    var 计算数3 = Marshal.ReadInt32(计算数 + 0xc);
			Print($"{计算数1}:{计算数2}:{计算数3}");
			log = new(DalamudApi.PluginInterface.ConfigDirectory);
			CastHook = new Hook<CastDelegate>(
			DalamudApi.SigScanner.ScanText("40 55 56 48 81 EC ?? ?? ?? ?? 48 8B EA"), StartCast);
			CastHook.Enable();
			ActorControlSelfHook = new Hook<ActorControlSelfDelegate>(
			DalamudApi.SigScanner.ScanText("E8 ?? ?? ?? ?? 0F B7 0B 83 E9 64"), ReceiveActorControlSelf);
			ActorControlSelfHook.Enable();
			this.pluginInterface.UiBuilder.Draw += BuildUI;
			this.pluginInterface.UiBuilder.OpenConfigUi += UiBuilder_OnOpenConfigUi;
			DalamudApi.Framework.Update += Update;
			LoadImage();

		}

		private void ReceiveActorControlSelf(uint entityId, uint type, uint buffID, uint direct, uint damage, uint sourceId,
											  uint arg4, uint arg5, ulong targetId, byte a10)
		{
			ActorControlSelfHook.Original(entityId, type, buffID, direct, damage, sourceId, arg4, arg5, targetId, a10);
			if (type == 0x6d)
			{
				PluginLog.Debug($"ActorControl{entityId:X4},{type:X4},{buffID:X4},{direct:X4},{damage:X4},{sourceId:X4},{arg4:X4},{arg5:X4},{targetId:X4},{a10:X4}");
				//初始化
				if (direct is 0x4000010 or 0x4000012 or 0x4000016)
				{

				}
			}
			if (type==34)
			{
				var actor = DalamudApi.Objects.Where(i => i.ObjectId == entityId).FirstOrDefault();
				var 计算数1 = Marshal.ReadInt32(计算数);
				var 计算数2 = Marshal.ReadInt32(计算数 + 4);
				var 计算数3 = Marshal.ReadInt32(计算数 + 0xc);
				log.WriteLog($"numble为{buffID}:{计算数1}:{计算数2}:{计算数3}");
				PluginLog.Log($"数值为{buffID}:{actor.Name.ToString()}");
				if (actor.ObjectId<0x40000000)
				{
					var abc = Math.Min(计算数3 + 计算数1 - 计算数2,0);
					var buffID1 = abc+buffID;
					//Raid.Mahjong((int)buffID1, actor);
					PluginLog.Log($"真实值为{buffID1}");
					log.WriteLog($"ture{buffID}:{buffID1}");
				}
				else
				{
					Raid.Mahjong((int)buffID, actor);
				}
			}
		}

		private void StartCast(uint source, IntPtr ptr)
		{
			try
			{
				CastHook.Original(source, ptr);
				if (source<0x40000000)
				{
					return;
				}
				var data = Marshal.PtrToStructure<ActorCast>(ptr);
				switch (data.action_id)
				{
					case 0x6625:
						Creadtime("普通魔锁");
						break;
					case 0x6626:
						Creadtime("四连魔锁");
						break;
					default:
						break;
				}
			}
			catch (Exception)
			{

				throw;
			}
			
		}
		
		public void LoadImage()
		{
			var texturePath = Path.Combine(pluginInterface.AssemblyLocation.DirectoryName, "Data", "numbers");
			var ABCD = new string[] { "A","B","C","D" };
			foreach (var abcd in ABCD)
			{
				var path3= Path.Combine(texturePath, "mark", abcd + ".png");
				_marksNumberTextures.Add(abcd, pluginInterface.UiBuilder.LoadImage(path3));
			}
			for (int i = 1; i < 8; i++)
			{
				try
				{
					//using var stream = File.OpenRead(Path.Combine(texturePath, i + ".png"));
					var path1= Path.Combine(texturePath, "red", i + ".png");
					var path2 = Path.Combine(texturePath, "yellow", i + ".png");
					_yellowNumberTextures.Add (i,pluginInterface.UiBuilder.LoadImage(path2));
					_redNumberTextures.Add(i, pluginInterface.UiBuilder.LoadImage(path1));
				}
				catch (Exception)
				{
					// the image does not exist or is invalid, we don't have to worry about it here
				}
			}
		}
		public static TextureWrap GetYellowTexture(int i)
		{
			if (i is>0 and <9)
			{
				return _yellowNumberTextures[i];
			}
			else
			{
				return null;
			}
		}
		public static TextureWrap GetRedTexture(int i)
		{
			if (i is > 0 and < 9)
			{
				return _redNumberTextures[i];
			}
			else
			{
				return null;
			}
		}
		public static TextureWrap GetMarkTexture(string a)
		{
			switch (a.ToUpper())
			{
				case "A":
				case "B":
				case "C":
				case "D":
					return _marksNumberTextures[a];
				default:
					return null;
			   break;
			}
		}

		private long headcall(IntPtr a, uint b)
		{
			var actor = DalamudApi.Objects.Where(i=>i.Address==a).FirstOrDefault();
			PluginLog.Log($"headcall的数值喂{b}");
			Raid.Mahjong((int)b, actor);
			return headCall.Original(a, b);
		}

		private void UiBuilder_OnOpenConfigUi()
		{
			config.ConfigUiVisible = true;
		}

		private void BuildUI()
		{
			if (config.ConfigUiVisible)
			{
				if (ImGui.Begin("FakeMark", ref config.ConfigUiVisible, ImGuiWindowFlags.AlwaysAutoResize))
				{
					if (ImGui.Checkbox("显示", ref config.Visable))
					{
						config.Save();
					}
					var texture = GetYellowTexture(2);
					var width = texture.Width * 1;
					ImGui.Image(texture.ImGuiHandle,
			   new Vector2(texture.Width * 1, texture.Height * 1));
					ImGui.SameLine();

				}
					
				//config.Save();
				ImGui.End();
			}
			try
			{
				if (DalamudApi.ClientState.LocalPlayer == null) return;

				var addon = XivApi.GetSafeAddonNamePlate();

				var bdl = ImGui.GetBackgroundDrawList(ImGui.GetMainViewport());
				foreach (var item in Marks)
				{
					if (item.Value.CreateTime.AddSeconds(item.Value.chiXuan).ToString()==DateTime.Now.ToString())
					{
						Marks.Remove(item.Key);
					 
					}
					var actors = DalamudApi.Objects.Where(i => i.ObjectId == item.Key.ObjectId);
					foreach (var actor in actors)
					{
						
						if (array is null)
						{
							return;
						}

						if (!array.TryGetValue((int)actor.ObjectId, out var namePlateObject)) continue;

						var node = namePlateObject.Data.RootNode;
						var pos = new Vector2(node->AtkResNode.X, node->AtkResNode.Y);
						var width = node->AtkResNode.Width;
						var height = node->AtkResNode.Height;
						//ImGui.PushFont(UiBuilder.DefaultFont);
						TextureWrap texture = item.Value.textureWrap;
						Vector2 texsize = new Vector2(width / 3, height * 6 / 7);
						DalamudApi.GameGui.WorldToScreen(actor.Position, out Vector2 pos1);
						foreach (var image in Icon)
						{
							if (image.Value== texture)
							{
								texsize = new Vector2(texture.Width, texture.Height)*1.5f; ;
							}
						}
						var screenPosOriginal = new Vector2(pos1.X - texsize.X / 2, pos.Y - 10);
						// var screenPosOriginal = new Vector2(pos.X + width * 1 / 2 - texsize.X/2-3, pos.Y-10);
						bdl.AddImage(texture.ImGuiHandle, screenPosOriginal, screenPosOriginal + texsize, Vector2.Zero, Vector2.One);



					}
				}
				
			}
			catch (Exception)
			{

				throw;
			}
		  
		   
		}



		public static void PrintEcho(string message) => DalamudApi.Chat.Print($"[FakeMark] {message}");
		public static void PrintError(string message) => DalamudApi.Chat.PrintError($"[FakeMark] {message}");
		public void Creadtime(string a)
		{
			createTime = DateTime.Now;
			Stage = a;
			Raid.NoOnce = true;
			Clean();
		}
		public static void Clean() => Marks.Clear();
		private void Update(Framework framework)
		{
			//var atkModule = (long)FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance()->UIModule->GetRaptureAtkModule();
			//Print(atkModule.ToString("X4"));
			try
			{
				if (DalamudApi.ClientState.LocalPlayer == null) return;
				if (!DalamudApi.Conditions[ConditionFlag.InCombat]) return;
				Raid.UpData();
				array = new Dictionary<int, XivApi.SafeNamePlateObject>();
				var addon = XivApi.GetSafeAddonNamePlate();
				for (int i = 0; i < 50; i++)
				{
					unsafe
					{
						var npObject = addon.GetNamePlateObject(i);
						if (npObject == null || !npObject.IsVisible)
							continue;

						var npInfo = npObject.NamePlateInfo;
						if (npInfo == null)
							continue;

						var actorID = npInfo.Data.ActorID;
						if (actorID == -1)
							continue;


						if (npObject.Data.NameplateKind != 3 && npObject.Data.NameplateKind != 0) continue;

						array.Add(actorID, npObject);
					}
				}
			}
			catch (Exception)
			{

				throw;
			}
		}


		[Command("/FakeMark")]
		[HelpMessage("显示boss相关")]
		public void FakeMarkCommand(string command, string args)
		{
			string[] array = args.Split(new char[]
	{
					' '
	});
			switch (array[0])
			{
				case "Clean":
					Clean();
					break;
				case "Add":
					Int32.TryParse(array[2],out var ID);
					if (uint.TryParse(array[1], out var objectID))
					{
						var actor = DalamudApi.Objects.Where(i=>i.ObjectId==objectID).FirstOrDefault();
						Raid.addMarks(actor,ID, colr.yellow);
					}
					else
					{
						var actor = DalamudApi.Objects.Where(i => i.Name.ToString() == array[1]).FirstOrDefault();
						Raid.addMarks(actor, ID, colr.yellow);
					}
					break;
				case "test":
					uint entityId = DalamudApi.ClientState.LocalPlayer.ObjectId;
					var actorAdress = DalamudApi.ClientState.LocalPlayer.Address;
					var targetId = 0xE0000000;
					var target = DalamudApi.ClientState.LocalPlayer.TargetObject.ObjectId;
					ReceiveActorControlSelf(entityId, 35, 0, 0, target, 0x66, 0, 0, targetId, 0);
				   // headcall(actorAdress, 275);
					//ReceiveActorControlSelf(entityId, 34, 275, 0, 0, 0, 0, 0, targetId, 0);
					//headCall.Original(actorAdress, 269);
					break;
				case "test1":
					FakeMark.Icon.TryGetValue(DalamudApi.ClientState.LocalPlayer.ClassJob.Id, out var texture);
					var marks = new Marks();
					marks.CreateTime = DateTime.Now;
					marks.textureWrap = texture;
					FakeMark.Marks.Add(DalamudApi.ClientState.LocalPlayer, marks);
					break;
				default:
					break;
			}
			string a = array[0];
		}
 

		public void Print(string a)
		{ DalamudApi.Chat.Print(a); }

		#region IDisposable Support
		protected virtual void Dispose(bool disposing)
		{
			if (!disposing) return;
			this.config.Save();
			this.pluginInterface.UiBuilder.Draw += BuildUI;
			this.pluginInterface.SavePluginConfig(this.config);
			this.pluginInterface.UiBuilder.OpenConfigUi -= UiBuilder_OnOpenConfigUi;
			DalamudApi.Framework.Update -= Update;
			CastHook.Disable();
			headCall.Disable();
			ActorControlSelfHook.Disable();
			DalamudApi.Dispose();
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		#endregion
	}
}
