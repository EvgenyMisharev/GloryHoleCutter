using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

                List<FamilyInstance> intersectionWallRectangularList = new List<FamilyInstance>();
                List<FamilyInstance> intersectionWallRoundList = new List<FamilyInstance>();
                List<FamilyInstance> intersectionFloorRectangularList = new List<FamilyInstance>();
                List<FamilyInstance> intersectionFloorRoundList = new List<FamilyInstance>();

                foreach (FamilyInstance gH in intersectionPointList)
                {
                    if(gH.Symbol.Family.Name.Equals("Пересечение_Стена_Прямоугольное"))
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
            }

            //Обработка прямоугольных пересечений со стеной

            //Обработка круглых пересечений со стеной

            //Обработка прямоугольных пересечений с перекрытием

            //Обработка круглых пересечений с перекрытием

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
