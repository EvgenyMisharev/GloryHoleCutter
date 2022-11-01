using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GloryHoleCutter
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    class GloryHoleCutterCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // Получение текущего документа
            Document doc = commandData.Application.ActiveUIDocument.Document;
            //Получение доступа к Selection
            Selection sel = commandData.Application.ActiveUIDocument.Selection;

            //Общие параметры размеров
            Guid intersectionPointWidthGuid = new Guid("8f2e4f93-9472-4941-a65d-0ac468fd6a5d");
            Guid intersectionPointHeightGuid = new Guid("da753fe3-ecfa-465b-9a2c-02f55d0c2ff1");
            Guid intersectionPointDiameterGuid = new Guid("9b679ab7-ea2e-49ce-90ab-0549d5aa36ff");

            Guid heightOfBaseLevelGuid = new Guid("9f5f7e49-616e-436f-9acc-5305f34b6933");
            Guid levelOffsetGuid = new Guid("515dc061-93ce-40e4-859a-e29224d80a10");

            Guid itemDiameter = new Guid("d6888dc7-7a03-40c3-9ac2-ab300c4e2c0a");
            Guid wallThickness = new Guid("de129aa9-dae8-430d-8de8-f7f08388fcaa");

            GloryHoleCutterWPF gloryHoleCutterWPF = new GloryHoleCutterWPF();
            gloryHoleCutterWPF.ShowDialog();
            if (gloryHoleCutterWPF.DialogResult != true)
            {
                return Result.Cancelled;
            }
            string useSleevesForRoundHolesButtonName = gloryHoleCutterWPF.UseSleevesForRoundHolesButtonName;

            FamilySymbol intersectionWallRectangularFamilySymbol = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_GenericModel)
                .OfClass(typeof(FamilySymbol))
                .Cast<FamilySymbol>()
                .FirstOrDefault(fs => fs.Family.Name.Equals("Отверстие_Стена_Прямоугольное"));
            if(intersectionWallRectangularFamilySymbol == null)
            {
                TaskDialog.Show("Ravit", "Не найден тип для прямоугольного отверстия в стене! Загрузите семейство \"Отверстие_Стена_Прямоугольное\"!");
                return Result.Cancelled;
            }

            FamilySymbol intersectionWallRoundFamilySymbol = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_GenericModel)
                .OfClass(typeof(FamilySymbol))
                .Cast<FamilySymbol>()
                .FirstOrDefault(fs => fs.Family.Name.Equals("Отверстие_Стена_Круглое"));
            if (intersectionWallRoundFamilySymbol == null)
            {
                TaskDialog.Show("Ravit", "Не найден тип для круглого отверстия в стене! Загрузите семейство \"Отверстие_Стена_Круглое\"!");
                return Result.Cancelled;
            }

            FamilySymbol intersectionFloorRectangularFamilySymbol = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_GenericModel)
                .OfClass(typeof(FamilySymbol))
                .Cast<FamilySymbol>()
                .FirstOrDefault(fs => fs.Family.Name.Equals("Отверстие_Плита_Прямоугольное"));
            if (intersectionFloorRectangularFamilySymbol == null)
            {
                TaskDialog.Show("Ravit", "Не найден тип для прямоугольного отверстия в плите! Загрузите семейство \"Отверстие_Плита_Прямоугольное\"!");
                return Result.Cancelled;
            }

            FamilySymbol intersectionFloorRoundFamilySymbol = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_GenericModel)
                .OfClass(typeof(FamilySymbol))
                .Cast<FamilySymbol>()
                .FirstOrDefault(fs => fs.Family.Name.Equals("Отверстие_Плита_Круглое"));
            if (intersectionFloorRoundFamilySymbol == null)
            {
                TaskDialog.Show("Ravit", "Не найден тип для круглого отверстия в плите! Загрузите семейство \"Отверстие_Плита_Круглое\"!");
                return Result.Cancelled;
            }

            List<FamilySymbol> sleeveFloorFamilySymbolList = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_GenericModel)
                .OfClass(typeof(FamilySymbol))
                .Cast<FamilySymbol>()
                .Where(fs => fs.Family.Name.Equals("Гильза_Плита"))
                .OrderBy(fs => fs.get_Parameter(itemDiameter).AsDouble())
                .ToList();
            if (sleeveFloorFamilySymbolList.Count.Equals(0))
            {
                TaskDialog.Show("Ravit", "Не найдены типы для гильз в плите! Загрузите семейство \"Гильза_Плита\"!");
                return Result.Cancelled;
            }

            List<FamilySymbol> sleeveWallFamilySymbolList = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_GenericModel)
                .OfClass(typeof(FamilySymbol))
                .Cast<FamilySymbol>()
                .Where(fs => fs.Family.Name.Equals("Гильза_Стена"))
                .OrderBy(fs => fs.get_Parameter(itemDiameter).AsDouble())
                .ToList();
            if (sleeveWallFamilySymbolList.Count.Equals(0))
            {
                TaskDialog.Show("Ravit", "Не найдены типы для гильз в стене! Загрузите семейство \"Гильза_Стена\"!");
                return Result.Cancelled;
            }

            //Получение точек вырезания
            List<FamilyInstance> intersectionPointList = new List<FamilyInstance>();
            intersectionPointList = GetIntersectionPointCurrentSelection(doc, sel);
            if (intersectionPointList.Count == 0)
            {
                //Выбор точек вырезания
                GloryHoleSelectionFilter gloryHoleSelectionFilter = new GloryHoleSelectionFilter();
                IList<Reference> intersectionPointRefList = null;
                try
                {
                    intersectionPointRefList = sel.PickObjects(ObjectType.Element, gloryHoleSelectionFilter, "Выберите точки вырезания!");
                }
                catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                {
                    return Result.Cancelled;
                }
                foreach (Reference refElem in intersectionPointRefList)
                {
                    intersectionPointList.Add((doc.GetElement(refElem) as FamilyInstance));
                }
            }

            List<FamilyInstance> intersectionWallRectangularList = new List<FamilyInstance>();
            List<FamilyInstance> intersectionWallRoundList = new List<FamilyInstance>();
            List<FamilyInstance> intersectionFloorRectangularList = new List<FamilyInstance>();
            List<FamilyInstance> intersectionFloorRoundList = new List<FamilyInstance>();

            foreach (FamilyInstance gH in intersectionPointList)
            {
                if (gH.Symbol.Family.Name.Equals("Пересечение_Стена_Прямоугольное"))
                {
                    intersectionWallRectangularList.Add(gH);
                }
                else if (gH.Symbol.Family.Name.Equals("Пересечение_Стена_Круглое"))
                {
                    intersectionWallRoundList.Add(gH);
                }
                else if (gH.Symbol.Family.Name.Equals("Пересечение_Плита_Прямоугольное"))
                {
                    intersectionFloorRectangularList.Add(gH);
                }
                else if (gH.Symbol.Family.Name.Equals("Пересечение_Плита_Круглое"))
                {
                    intersectionFloorRoundList.Add(gH);
                }
            }

            using (TransactionGroup tg = new TransactionGroup(doc))
            {
                Options opt = new Options();
                opt.ComputeReferences = true;
                opt.DetailLevel = ViewDetailLevel.Fine;

                tg.Start("Вырезание отверстий");
                using (Transaction t = new Transaction(doc))
                {
                    t.Start("Активация FamilySymbo");
                    if (intersectionWallRectangularFamilySymbol != null)
                    {
                        intersectionWallRectangularFamilySymbol.Activate();
                    }
                    if (intersectionWallRoundFamilySymbol != null)
                    {
                        intersectionWallRoundFamilySymbol.Activate();
                    }
                    if (intersectionFloorRectangularFamilySymbol != null)
                    {
                        intersectionFloorRectangularFamilySymbol.Activate();
                    }
                    if (intersectionFloorRoundFamilySymbol != null)
                    {
                        intersectionFloorRoundFamilySymbol.Activate();
                    }
                    t.Commit();
                }    

                //Обработка прямоугольных пересечений со стеной
                using (Transaction t = new Transaction(doc))
                {
                    t.Start("Обработка прямоугольных пересечений со стеной");
                    List<FamilyInstance> intersectionWallRectangularForDelList = new List<FamilyInstance>();
                    foreach (FamilyInstance gH in intersectionWallRectangularList)
                    {
                        BoundingBoxXYZ bb = gH.get_BoundingBox(null);
                        Outline myOutLn = new Outline(bb.Min, bb.Max);
                        BoundingBoxIntersectsFilter filter = new BoundingBoxIntersectsFilter(myOutLn);
                        //Получение стен
                        List<Wall> wallsList = new FilteredElementCollector(doc)
                            .OfCategory(BuiltInCategory.OST_Walls)
                            .OfClass(typeof(Wall))
                            .WhereElementIsNotElementType()
                            .WherePasses(filter)
                            .Cast<Wall>()
                            .ToList();

                        foreach (Wall wall in wallsList)
                        {
                            GeometryElement geomElem = wall.get_Geometry(opt);
                            foreach (GeometryObject geomObj in geomElem)
                            {
                                Solid wallSolid = geomObj as Solid;
                                if (null != wallSolid)
                                {
                                    Solid pointSolid = GetSolidFromIntersectionPoint(opt, gH);
                                    double intersectvolume = Math.Round(BooleanOperationsUtils.ExecuteBooleanOperation(wallSolid, pointSolid, BooleanOperationsType.Intersect).Volume, 6);
                                    if (intersectvolume > 0)
                                    {
                                        FamilyInstance hole = doc.Create.NewFamilyInstance((gH.Location as LocationPoint).Point
                                            , intersectionWallRectangularFamilySymbol
                                            , wall
                                            , doc.GetElement(gH.LevelId) as Level
                                            , StructuralType.NonStructural) as FamilyInstance;

                                        hole.get_Parameter(intersectionPointWidthGuid).Set(gH.get_Parameter(intersectionPointWidthGuid).AsDouble());
                                        hole.get_Parameter(intersectionPointHeightGuid).Set(gH.get_Parameter(intersectionPointHeightGuid).AsDouble());
                                        hole.get_Parameter(heightOfBaseLevelGuid).Set(gH.get_Parameter(heightOfBaseLevelGuid).AsDouble());
                                        hole.get_Parameter(levelOffsetGuid).Set(gH.get_Parameter(levelOffsetGuid).AsDouble());
                                        intersectionWallRectangularForDelList.Add(gH);
                                    }
                                }
                            }
                        }
                    }
                    foreach (FamilyInstance gH in intersectionWallRectangularForDelList)
                    {
                        try
                        {
                            doc.Delete(gH.Id);
                        }
                        catch
                        {
                            //Если элемент уже удалён
                        }
                    }
                    t.Commit();
                }

                //Обработка круглых пересечений со стеной
                List<FamilyInstance> intersectionWallRoundForDelList = new List<FamilyInstance>();
                foreach (FamilyInstance gH in intersectionWallRoundList)
                {
                    BoundingBoxXYZ bb = gH.get_BoundingBox(null);
                    Outline myOutLn = new Outline(bb.Min, bb.Max);
                    BoundingBoxIntersectsFilter filter = new BoundingBoxIntersectsFilter(myOutLn);
                    //Получение стен
                    List<Wall> wallsList = new FilteredElementCollector(doc)
                        .OfCategory(BuiltInCategory.OST_Walls)
                        .OfClass(typeof(Wall))
                        .WhereElementIsNotElementType()
                        .WherePasses(filter)
                        .Cast<Wall>()
                        .ToList();

                    foreach (Wall wall in wallsList)
                    {
                        GeometryElement geomElem = wall.get_Geometry(opt);
                        foreach (GeometryObject geomObj in geomElem)
                        {
                            Solid wallSolid = geomObj as Solid;
                            if (null != wallSolid)
                            {
                                Solid pointSolid = GetSolidFromIntersectionPoint(opt, gH);
                                double intersectvolume = Math.Round(BooleanOperationsUtils.ExecuteBooleanOperation(wallSolid, pointSolid, BooleanOperationsType.Intersect).Volume, 6);
                                if (intersectvolume > 0)
                                {
                                    if(useSleevesForRoundHolesButtonName == "radioButton_UseSleevesForRoundHolesYes")
                                    {
                                        FamilySymbol sleeveWallFamilySymbol = null;
                                        foreach (FamilySymbol fs in sleeveWallFamilySymbolList)
                                        {
                                            if ((fs.get_Parameter(itemDiameter).AsDouble() - fs.get_Parameter(wallThickness).AsDouble() * 2) >= gH.get_Parameter(intersectionPointDiameterGuid).AsDouble())
                                            {
                                                sleeveWallFamilySymbol = fs;
                                                break;
                                            }
                                        }

                                        using (Transaction t = new Transaction(doc))
                                        {
                                            t.Start("Временная транзакция");
                                            sleeveWallFamilySymbol.Activate();
                                            FamilyInstance holeTmp = doc.Create.NewFamilyInstance((gH.Location as LocationPoint).Point
                                                , sleeveWallFamilySymbol
                                                , wall
                                                , doc.GetElement(gH.LevelId) as Level
                                                , StructuralType.NonStructural) as FamilyInstance;
                                            doc.Delete(holeTmp.Id);
                                            t.Commit();
                                        }

                                        using (Transaction t = new Transaction(doc))
                                        {
                                            t.Start("Обработка круглых пересечений со стеной");
                                            FamilyInstance hole = doc.Create.NewFamilyInstance((gH.Location as LocationPoint).Point
                                                , sleeveWallFamilySymbol
                                                , wall
                                                , doc.GetElement(gH.LevelId) as Level
                                                , StructuralType.NonStructural) as FamilyInstance;

                                            hole.get_Parameter(heightOfBaseLevelGuid).Set(gH.get_Parameter(heightOfBaseLevelGuid).AsDouble());
                                            hole.get_Parameter(levelOffsetGuid).Set(gH.get_Parameter(levelOffsetGuid).AsDouble());
                                            intersectionWallRoundForDelList.Add(gH);
                                            t.Commit();
                                        }

                                    }
                                    else
                                    {
                                        using (Transaction t = new Transaction(doc))
                                        {
                                            t.Start("Обработка круглых пересечений со стеной");
                                            FamilyInstance hole = doc.Create.NewFamilyInstance((gH.Location as LocationPoint).Point
                                                , intersectionWallRoundFamilySymbol
                                                , wall
                                                , doc.GetElement(gH.LevelId) as Level
                                                , StructuralType.NonStructural) as FamilyInstance;

                                            hole.get_Parameter(intersectionPointDiameterGuid).Set(gH.get_Parameter(intersectionPointDiameterGuid).AsDouble());
                                            hole.get_Parameter(heightOfBaseLevelGuid).Set(gH.get_Parameter(heightOfBaseLevelGuid).AsDouble());
                                            hole.get_Parameter(levelOffsetGuid).Set(gH.get_Parameter(levelOffsetGuid).AsDouble());
                                            intersectionWallRoundForDelList.Add(gH);
                                            t.Commit();
                                        }    
                                    }
                                }
                            }
                        }
                    }
                }
                using (Transaction t = new Transaction(doc))
                {
                    t.Start("Удаление болванок");
                    foreach (FamilyInstance gH in intersectionWallRoundForDelList)
                    {
                        try
                        {
                            doc.Delete(gH.Id);
                        }
                        catch
                        {
                            //Если элемент уже удалён
                        }
                    }
                    t.Commit();
                }

                //Обработка прямоугольных пересечений с перекрытием
                using (Transaction t = new Transaction(doc))
                {
                    t.Start("Обработка прямоугольных пересечений с перекрытием");
                    List<FamilyInstance> intersectionFloorRectangularForDelList = new List<FamilyInstance>();
                    foreach (FamilyInstance gH in intersectionFloorRectangularList)
                    {
                        BoundingBoxXYZ bb = gH.get_BoundingBox(null);
                        Outline myOutLn = new Outline(bb.Min, bb.Max);
                        BoundingBoxIntersectsFilter filter = new BoundingBoxIntersectsFilter(myOutLn);
                        //Получение перекрытий
                        List<Floor> floorsList = new FilteredElementCollector(doc)
                            .OfCategory(BuiltInCategory.OST_Floors)
                            .OfClass(typeof(Floor))
                            .WhereElementIsNotElementType()
                            .WherePasses(filter)
                            .Cast<Floor>()
                            .Where(f => f.get_Parameter(BuiltInParameter.FLOOR_PARAM_IS_STRUCTURAL).AsInteger() == 1)
                            .ToList();

                        foreach (Floor floor in floorsList)
                        {
                            GeometryElement geomElem = floor.get_Geometry(opt);
                            foreach (GeometryObject geomObj in geomElem)
                            {
                                Solid floorSolid = geomObj as Solid;
                                if (null != floorSolid)
                                {
                                    Solid pointSolid = GetSolidFromIntersectionPoint(opt, gH);
                                    double intersectvolume = Math.Round(BooleanOperationsUtils.ExecuteBooleanOperation(floorSolid, pointSolid, BooleanOperationsType.Intersect).Volume, 6);
                                    if (intersectvolume > 0)
                                    {
                                        FamilyInstance hole = doc.Create.NewFamilyInstance((gH.Location as LocationPoint).Point
                                            , intersectionFloorRectangularFamilySymbol
                                            , floor
                                            , doc.GetElement(gH.LevelId) as Level
                                            , StructuralType.NonStructural) as FamilyInstance;

                                        hole.get_Parameter(intersectionPointWidthGuid).Set(gH.get_Parameter(intersectionPointWidthGuid).AsDouble());
                                        hole.get_Parameter(intersectionPointHeightGuid).Set(gH.get_Parameter(intersectionPointHeightGuid).AsDouble());
                                        if (Math.Round((gH.Location as LocationPoint).Rotation, 6) != 0)
                                        {
                                            Line axis = Line.CreateBound((gH.Location as LocationPoint).Point, (gH.Location as LocationPoint).Point + 1 * XYZ.BasisZ);
                                            ElementTransformUtils.RotateElement(doc, hole.Id, axis, (gH.Location as LocationPoint).Rotation);
                                        }
                                        hole.get_Parameter(heightOfBaseLevelGuid).Set((doc.GetElement(hole.LevelId) as Level).Elevation);
                                        hole.get_Parameter(levelOffsetGuid).Set(floor.get_Parameter(BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM).AsDouble());
                                        intersectionFloorRectangularForDelList.Add(gH);
                                    }
                                }
                            }
                        }
                    }
                    foreach (FamilyInstance gH in intersectionFloorRectangularForDelList)
                    {
                        try
                        {
                            doc.Delete(gH.Id);
                        }
                        catch
                        {
                            //Если элемент уже удалён
                        }
                    }
                    t.Commit();
                }    

                //Обработка круглых пересечений с перекрытием
                List<FamilyInstance> intersectionFloorRoundForDelList = new List<FamilyInstance>();
                foreach (FamilyInstance gH in intersectionFloorRoundList)
                {
                    BoundingBoxXYZ bb = gH.get_BoundingBox(null);
                    Outline myOutLn = new Outline(bb.Min, bb.Max);
                    BoundingBoxIntersectsFilter filter = new BoundingBoxIntersectsFilter(myOutLn);
                    //Получение перекрытий
                    List<Floor> floorsList = new FilteredElementCollector(doc)
                        .OfCategory(BuiltInCategory.OST_Floors)
                        .OfClass(typeof(Floor))
                        .WhereElementIsNotElementType()
                        .WherePasses(filter)
                        .Cast<Floor>()
                        .Where(f => f.get_Parameter(BuiltInParameter.FLOOR_PARAM_IS_STRUCTURAL).AsInteger() == 1)
                        .ToList();

                    foreach (Floor floor in floorsList)
                    {
                        GeometryElement geomElem = floor.get_Geometry(opt);
                        foreach (GeometryObject geomObj in geomElem)
                        {
                            Solid floorSolid = geomObj as Solid;
                            if (null != floorSolid)
                            {
                                Solid pointSolid = GetSolidFromIntersectionPoint(opt, gH);
                                double intersectvolume = Math.Round(BooleanOperationsUtils.ExecuteBooleanOperation(floorSolid, pointSolid, BooleanOperationsType.Intersect).Volume, 6);
                                if (intersectvolume > 0)
                                {
                                    if (useSleevesForRoundHolesButtonName == "radioButton_UseSleevesForRoundHolesYes")
                                    {
                                        FamilySymbol sleeveFloorFamilySymbol = null;
                                        foreach (FamilySymbol fs in sleeveFloorFamilySymbolList)
                                        {
                                            if ((fs.get_Parameter(itemDiameter).AsDouble() - fs.get_Parameter(wallThickness).AsDouble() * 2) >= gH.get_Parameter(intersectionPointDiameterGuid).AsDouble())
                                            {
                                                sleeveFloorFamilySymbol = fs;
                                                break;
                                            }
                                        }
                                        using (Transaction t = new Transaction(doc))
                                        {
                                            t.Start("Временная транзакция");
                                            sleeveFloorFamilySymbol.Activate();
                                            FamilyInstance holeTmp = doc.Create.NewFamilyInstance((gH.Location as LocationPoint).Point
                                                , sleeveFloorFamilySymbol
                                                , floor
                                                , doc.GetElement(gH.LevelId) as Level
                                                , StructuralType.NonStructural) as FamilyInstance;
                                            doc.Delete(holeTmp.Id);
                                            t.Commit();
                                        }
                                        using (Transaction t = new Transaction(doc))
                                        {
                                            t.Start("Обработка круглых пересечений с перекрытием");
                                            FamilyInstance hole = doc.Create.NewFamilyInstance((gH.Location as LocationPoint).Point
                                                , sleeveFloorFamilySymbol
                                                , floor
                                                , doc.GetElement(gH.LevelId) as Level
                                                , StructuralType.NonStructural) as FamilyInstance;

                                            hole.get_Parameter(heightOfBaseLevelGuid).Set((doc.GetElement(hole.LevelId) as Level).Elevation);
                                            hole.get_Parameter(levelOffsetGuid).Set(floor.get_Parameter(BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM).AsDouble());
                                            intersectionFloorRoundForDelList.Add(gH);
                                            t.Commit();
                                        }
                                    }
                                    else
                                    {
                                        using (Transaction t = new Transaction(doc))
                                        {
                                            t.Start("Обработка круглых пересечений с перекрытием");
                                            FamilyInstance hole = doc.Create.NewFamilyInstance((gH.Location as LocationPoint).Point
                                                , intersectionFloorRoundFamilySymbol
                                                , floor
                                                , doc.GetElement(gH.LevelId) as Level
                                                , StructuralType.NonStructural) as FamilyInstance;

                                            hole.get_Parameter(intersectionPointDiameterGuid).Set(gH.get_Parameter(intersectionPointDiameterGuid).AsDouble());
                                            hole.get_Parameter(heightOfBaseLevelGuid).Set((doc.GetElement(hole.LevelId) as Level).Elevation);
                                            hole.get_Parameter(levelOffsetGuid).Set(floor.get_Parameter(BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM).AsDouble());
                                            intersectionFloorRoundForDelList.Add(gH);
                                            t.Commit();
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                using (Transaction t = new Transaction(doc))
                {
                    t.Start("Удаление болванок");
                    foreach (FamilyInstance gH in intersectionFloorRoundForDelList)
                    {
                        try
                        {
                            doc.Delete(gH.Id);
                        }
                        catch
                        {
                            //Если элемент уже удалён
                        }
                    }
                    t.Commit();
                }
                tg.Assimilate();
            }
            return Result.Succeeded;
        }

        private static Solid GetSolidFromIntersectionPoint(Options opt, FamilyInstance fi)
        {
            Solid tmpSolid = null;
            GeometryElement geoElement = fi.get_Geometry(opt);
            foreach (GeometryObject geoObject in geoElement)
            {
                GeometryInstance instance = geoObject as GeometryInstance;
                if (instance != null)
                {
                    GeometryElement instanceGeometryElement = instance.GetInstanceGeometry();
                    foreach (GeometryObject o in instanceGeometryElement)
                    {
                        tmpSolid = o as Solid;
                        if (tmpSolid != null && tmpSolid.Volume != 0) break;
                    }
                }
            }

            return tmpSolid;
        }
        private static List<FamilyInstance> GetIntersectionPointCurrentSelection(Document doc, Selection sel)
        {
            ICollection<ElementId> selectedIds = sel.GetElementIds();
            List<FamilyInstance> tempIntersectionPointList = new List<FamilyInstance>();
            foreach (ElementId intersectionPointId in selectedIds)
            {
                if (doc.GetElement(intersectionPointId) is FamilyInstance
                    && null != doc.GetElement(intersectionPointId).Category
                    && doc.GetElement(intersectionPointId).Category.Id.IntegerValue.Equals((int)BuiltInCategory.OST_GenericModel)
                    && ((doc.GetElement(intersectionPointId) as FamilyInstance).Symbol.Family.Name.Equals("Пересечение_Стена_Прямоугольное")
                    || (doc.GetElement(intersectionPointId) as FamilyInstance).Symbol.Family.Name.Equals("Пересечение_Стена_Круглое")
                    || (doc.GetElement(intersectionPointId) as FamilyInstance).Symbol.Family.Name.Equals("Пересечение_Плита_Прямоугольное")
                    || (doc.GetElement(intersectionPointId) as FamilyInstance).Symbol.Family.Name.Equals("Пересечение_Плита_Круглое")))

                {
                    tempIntersectionPointList.Add(doc.GetElement(intersectionPointId) as FamilyInstance);
                }
            }
            return tempIntersectionPointList;
        }
    }
}
