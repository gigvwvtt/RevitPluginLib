using System;
using System.Linq;
using System.Windows.Forms;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI.Selection;

namespace RevitPlugin
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class RotateObjects : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var uiApp = commandData.Application;
            var uiDoc = uiApp.ActiveUIDocument;
            var doc = uiDoc.Document;

            try
            {
                var pickedObjects = uiDoc.Selection
                    .PickObjects(ObjectType.Element).Select(x => x.ElementId).ToList();

                UserDataCollector.CollectDataInput("Введите угол поворота в градусах", out var angle);
                angle *= Math.PI / 180;

                using (var t = new Transaction(doc, "RotateElement"))
                {
                    t.Start();
                    var lineAsAxis = Line.CreateBound(XYZ.Zero, XYZ.BasisZ);
                    ElementTransformUtils.RotateElements(doc, pickedObjects, lineAsAxis, angle);
                    t.Commit();
                }
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return Result.Cancelled;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }

            return Result.Succeeded;
        }
    }

    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class CopyObjects : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var uiApp = commandData.Application;
            var uiDoc = uiApp.ActiveUIDocument;
            var doc = uiDoc.Document;

            try
            {
                var pickedObjects = uiDoc.Selection
                    .PickObjects(ObjectType.Element).Select(x => x.ElementId).ToList();
                using (var t = new Transaction(doc, "CopyElement"))
                {
                    t.Start();
                    var mouseClickPoint = uiDoc.Selection.PickPoint();
                    ElementTransformUtils.CopyElements(doc, pickedObjects, mouseClickPoint);
                    t.Commit();
                }
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return Result.Cancelled;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }

            return Result.Succeeded;
        }
    }

    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class StretchWall : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var uiApp = commandData.Application;
            var uiDoc = uiApp.ActiveUIDocument;
            var doc = uiDoc.Document;

            try
            {
                var pickedObject = uiDoc.Selection
                    .PickObject(ObjectType.Element, new WallPickFilter(), "Выберите стену");
                UserDataCollector.CollectDataInput("Введите значение в миллиметрах", out var desiredHeight);
                const double foot = 3.2808d;
                var height = desiredHeight * foot / 1000; //в футах

                using (var t = new Transaction(doc, "stretch"))
                {
                    t.Start();
                    var wall = doc.GetElement(pickedObject) as Wall;
                    wall?.get_Parameter(BuiltInParameter.WALL_USER_HEIGHT_PARAM).Set(height);
                    t.Commit();
                }
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return Result.Cancelled;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }

            return Result.Succeeded;
        }
    }

    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class MoveGroupToRoomCenter : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var uiApp = commandData.Application;
            var uiDoc = uiApp.ActiveUIDocument;
            var doc = uiDoc.Document;

            try
            {
                var pickedObject = uiDoc.Selection
                    .PickObject(ObjectType.Element, new GroupPickFilter(), "Выберите группу объектов");

                if (pickedObject != null && pickedObject.ElementId != ElementId.InvalidElementId)
                {
                    var bounding = doc.GetElement(pickedObject).get_BoundingBox(null);
                    var center = (bounding.Max + bounding.Min) * 0.5;
                    var translationVector = XYZ.Zero - center;

                    using (var t = new Transaction(doc, "move"))
                    {
                        t.Start("move");
                        ElementTransformUtils.MoveElement(doc, pickedObject.ElementId, translationVector);
                        t.Commit();
                    }
                }
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return Result.Cancelled;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }

            return Result.Succeeded;
        }
    }

    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class GetAreaOfConnectedWalls : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var uiApp = commandData.Application;
            var uiDoc = uiApp.ActiveUIDocument;
            var doc = uiDoc.Document;

            try
            {
                var pickedObjects = uiDoc.Selection
                    .PickObjects(ObjectType.Element, new WallPickFilter(),"Выберите две стены");
                
                using (var t = new Transaction(doc, "GetArea"))
                {
                    t.Start();
                    var element1 = doc.GetElement(pickedObjects[0].ElementId);
                    var element2 = doc.GetElement(pickedObjects[1].ElementId);
                    
                    //не работает
                    var areJoined = JoinGeometryUtils.AreElementsJoined(doc,element1, element2);
                    
                    var connectedArea = 0d;
                    if (areJoined)
                    {
                        connectedArea = element1.get_Parameter(BuiltInParameter.HOST_AREA_COMPUTED).AsDouble() + 
                                         element2.get_Parameter(BuiltInParameter.HOST_AREA_COMPUTED).AsDouble();
                    }
                    
                    TaskDialog.Show("Площадь двух соприкасающихся стен",$"{connectedArea.ToString()} м2");
                    t.Commit();
                }
                
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return Result.Cancelled;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }

            return Result.Succeeded;
        }
    }
}

public class GroupPickFilter : ISelectionFilter
{
    public bool AllowElement(Element e)
    {
        return (e.Category.Id.IntegerValue.Equals(
            (int)BuiltInCategory.OST_IOSModelGroups));
    }

    public bool AllowReference(Reference r, XYZ p)
    {
        return false;
    }
}

public class WallPickFilter : ISelectionFilter
{
    public bool AllowElement(Element e)
    {
        return (e.Category.Id.IntegerValue.Equals(
            (int)BuiltInCategory.OST_Walls));
    }

    public bool AllowReference(Reference r, XYZ p)
    {
        return false;
    }
}

public static class UserDataCollector

{
    public static bool CollectDataInput(string title, out double ret)
    {
        var dc = new System.Windows.Forms.Form()
        {
            Text = title,
            HelpButton = false,
            MinimizeBox = false,
            MaximizeBox = false,
            ShowIcon = false,
            ShowInTaskbar = false,
            TopMost = true,
            Height = 100,
            Width = 300,
        };
        dc.MinimumSize = new System.Drawing.Size(dc.Width, dc.Height);

        var margin = 5;
        var size = dc.ClientSize;

        var textBox = new System.Windows.Forms.TextBox()
        {
            TextAlign = HorizontalAlignment.Right,
            Height = 20,
            Width = size.Width - 2 * margin,
            Location = new System.Drawing.Point(margin, margin),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        dc.Controls.Add(textBox);
        var ok = new Button()
        {
            Text = "OK",
            Height = 23,
            Width = 75,
            Anchor = AnchorStyles.Bottom
        };
        ok.Click += ok_Click;
        ok.Location = new System.Drawing.Point(size.Width / 2 - ok.Width / 2, size.Height / 2);
        dc.Controls.Add(ok);
        dc.AcceptButton = ok;

        dc.ShowDialog();

        return double.TryParse(textBox.Text, out ret);
    }

    private static void ok_Click(object sender, EventArgs e)
    {
        var form = ((System.Windows.Forms.Control)sender).Parent as System.Windows.Forms.Form;
        if (form == null) return;
        form.DialogResult = DialogResult.OK;
        form.Close();
    }
}