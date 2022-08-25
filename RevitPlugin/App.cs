using System.Reflection;
using Autodesk.Revit.UI;

namespace RevitPlugin
{
    public class App : IExternalApplication

    {
        public Result OnStartup(UIControlledApplication application)
        {
            var tabName = "PluginTest";
            var panelName = "testPanel";
            var assemblyPath = Assembly.GetExecutingAssembly();

            application.CreateRibbonTab(tabName);
            var panel = application.CreateRibbonPanel(tabName, panelName);
            panel.AddItem(new PushButtonData("RotateObjects", "Повернуть объекты",
                assemblyPath.Location, "RevitPlugin.RotateObjects"))
                .ToolTip = "Выберите объекты";
            panel.AddItem(new PushButtonData("MoveToRoomCenter", "Переместить объекты в центр координат",
                assemblyPath.Location, "RevitPlugin.MoveGroupToRoomCenter"))
                .ToolTip = "Выберите группу объектов";
            panel.AddItem(new PushButtonData("CopyObjects", "Копировать объекты",
                assemblyPath.Location, "RevitPlugin.CopyObjects"))
                .ToolTip = "Выберите объекты, затем нажмите в пространстве, куда хотите вставить их";
            panel.AddItem(new PushButtonData("StretchWall", "Растянуть стену вдоль",
                assemblyPath.Location, "RevitPlugin.StretchWall"))
                .ToolTip = "Выберите стену";
            panel.AddItem(new PushButtonData("GetAreOfWalls", "Площадь соприкасающихся стен",
                assemblyPath.Location, "RevitPlugin.GetAreaOfConnectedWalls"))
                .ToolTip = "Выберите 2 стены";
            
            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }
    }
}