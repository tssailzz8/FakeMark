using Dalamud.Configuration;
using System.Linq;
using System.Numerics;

namespace FakeMark
{
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; }
        public void Initialize() { }
        public bool ConfigUiVisible;
        public int xianshi=1;
        public bool Visable;
        public bool InCombat;
        public Vector4 颜色;
        public bool 竹子;
        public bool 自己;
        public bool 登录;
        public bool 敌人圈;
        public bool 聊天地图;
        public bool 狩猎;
        public Vector2 WindowPos = new(50, 50);
        public Vector4[] KindColorsBg = Enumerable.Repeat(new Vector4(0, 0, 0, 0.75f), 20).ToArray();
        public void Save() => DalamudApi.PluginInterface.SavePluginConfig(this);
    }
}
