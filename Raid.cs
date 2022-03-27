using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Logging;
using FakeMark.Struck;
using ImGuiScene;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FakeMark
{
    public abstract class Raid
    {
        public enum colr
        {
            red=1,
            yellow=2,
            mark=3
        }

        public static ushort 位置;
        public static bool NoOnce = true;

        public static float ElapsedSeconds => (float)(DateTime.Now - FakeMark.createTime).TotalSeconds;

        public Raid()
        {
            位置 = DalamudApi.ClientState.TerritoryType;
        }
        public static void UpData()
        {
            P4SLine();
            P1SDebuff();
        }
        public static void Mahjong(int a, Dalamud.Game.ClientState.Objects.Types.GameObject actor)
        {
            int markID;
          
            switch (a)
            {
                case >= 268 and <= 271:
                markID = a - 267;
                    addMarks(actor, markID, colr.red);
                    break;
                case >= 272 and <= 275:
                    markID = a - 271;
                    addMarks(actor, markID, colr.yellow);
                    break;
                case >= 145 and <= 148:
                    markID = a - 144;
                    addMarks(actor, markID, colr.red);
                    break;
                case >= 149 and <= 152:
                    markID = a - 148;
                    addMarks(actor, markID, colr.yellow);
                    break;
                case >= 79 and <= 82:
                    markID = a - 78;
                    addMarks(actor, markID, colr.yellow);
                    break;
                case >= 83 and <= 86:
                    markID = a - 82;
                    addMarks(actor, markID, colr.red);
                    break;
                default:
                    break;
            }

        }

        public static void addMarks(Dalamud.Game.ClientState.Objects.Types.GameObject a, int b,colr colr,float chixu=10)
        {
            if (FakeMark.Marks.ContainsKey(a)) return;
            TextureWrap texture=default;
            var marks = new Marks();
            marks.CreateTime = DateTime.Now;
            marks.chiXuan = chixu;
            switch (colr)
            {
                case colr.red:

                    texture = FakeMark.GetRedTexture(b);
                    marks.textureWrap = texture;
                    FakeMark.Marks.Add(a, marks);
                    break;
                case colr.yellow:
                     texture = FakeMark.GetYellowTexture(b);
                    marks.textureWrap = texture;
                    FakeMark.Marks.Add(a, marks);
                    break;
                case colr.mark:
                    switch (b)
                    {
                        case 1:
                            texture = FakeMark.GetMarkTexture("A");
                            break;
                        case 2:
                            texture = FakeMark.GetMarkTexture("B");
                            break;
                        case 3:
                            texture = FakeMark.GetMarkTexture("C");
                            break;
                        case 4:
                            texture = FakeMark.GetMarkTexture("D");
                            break;
                        default:
                            break;
                    }
                    marks.textureWrap = texture;
                    FakeMark.Marks.Add(a, marks);
                    break;
                default:
                break;
            }
        }
        public static void P4SLine()
        {
            //if (位置 != 1003) return;

            var data = Marshal.PtrToStructure<Tether>(DalamudApi.ClientState.LocalPlayer.Address);
            if (data.ID!= 0)
            {
                var targetObject = DalamudApi.Objects.Where(i=>i.ObjectId==data.targetID).FirstOrDefault();
                var job = Marshal.ReadByte(targetObject.Address, 0x1e0);
                
                if (FakeMark.Icon.TryGetValue(job, out var texture1))
                {
                    if (FakeMark.Marks.ContainsKey(DalamudApi.ClientState.LocalPlayer)) return;
                    if (job==0) return;
                    PluginLog.Log($"{job}:{data.ID}:{data.length}:{data.targetID:X}");
                    var marks = new Marks();
                    marks.CreateTime = DateTime.Now;
                    marks.chiXuan = 10f;
                    marks.textureWrap = texture1;
                    FakeMark.Marks.Add(DalamudApi.ClientState.LocalPlayer, marks);
                }
                //if (targetObject is PlayerCharacter character)
                //{
                //    if (FakeMark.Icon.TryGetValue(character.ClassJob.Id, out var texture))
                //    {
                //        if (FakeMark.Marks.ContainsKey(DalamudApi.ClientState.LocalPlayer)) return;
                //        var marks = new Marks();
                //        marks.textureWrap = texture;
                //        FakeMark.Marks.Add(DalamudApi.ClientState.LocalPlayer, marks);
                //    }
                    
                //}
               
            }
        }
        public static void P1SDebuff()
        {
            //只触发一次
            if (DalamudApi.Objects is null)
            {
                return;
            }

            if (FakeMark.Stage == "普通魔锁")
                {
                    var jin = FindBuff(0XAB6);
                    var yuan= FindBuff(0XAB7);
                    if (jin is not null && yuan is not null)
                    {
                        addMarks(jin,1,colr.mark);
                        addMarks(yuan, 1, colr.red);
                        NoOnce = false;
                    }

                }
                if (FakeMark.Stage == "四连魔锁")
                {
                    var jin3 = FindBuff(0XB45);
                    if (jin3 is not null) addMarks(jin3,1,colr.yellow);
                    var jin8 = FindBuff(0xB46);
                    if (jin8 is not null) addMarks(jin8, 2, colr.yellow);
                    var jin13 = FindBuff(0XB47);
                    if (jin13 is not null) addMarks(jin13, 3, colr.yellow);
                    var jin18 = FindBuff(0XB6B);
                    if (jin18 is not null) addMarks(jin18, 4, colr.yellow);
                    var yuan3= FindBuff(0XB48);
                    if (yuan3 is not null) addMarks(yuan3, 1, colr.mark);
                    var yuan8 = FindBuff(0XB49);
                    if (yuan8 is not null) addMarks(yuan8, 2, colr.mark);
                    var yuan13 = FindBuff(0XB4A);
                    if (yuan13 is not null) addMarks(yuan13, 3, colr.mark);
                    var yuan18 = FindBuff(0XB6C);
                    if (yuan18 is not null) addMarks(yuan18, 4, colr.mark);

            }

              
           
        }
        public static Dalamud.Game.ClientState.Objects.Types.GameObject FindBuff(ushort effectID)
        {
            if (DalamudApi.Objects is null)
            {
                return null;
            }
            var objects = DalamudApi.Objects?.Where(i => i.ObjectKind == ObjectKind.Player );
            if (objects is  not  null)
            {
                foreach (var actor1 in objects)
                {
                    var actor= (BattleChara)actor1;
                    foreach (var status in actor.StatusList)
                    {
                        if (status.StatusId == effectID)
                        {
                            return actor;
                        }
                    }
                }
            }

            return null;
        }
    }
}
