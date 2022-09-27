using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;
using System;

namespace GloryHoleCutter
{
    class GloryHoleSelectionFilter : ISelectionFilter
    {
		public bool AllowElement(Autodesk.Revit.DB.Element elem)
		{
			if (elem is FamilyInstance
				&& elem.Category.Id.IntegerValue == (int)BuiltInCategory.OST_GenericModel
				&& ((elem as FamilyInstance).Symbol.Family.Name.Equals("Пересечение_Стена_Прямоугольное")
				|| (elem as FamilyInstance).Symbol.Family.Name.Equals("Пересечение_Стена_Круглое")
				|| (elem as FamilyInstance).Symbol.Family.Name.Equals("Пересечение_Плита_Прямоугольное")
				|| (elem as FamilyInstance).Symbol.Family.Name.Equals("Пересечение_Плита_Круглое")))
			{
				return true;
			}
			return false;
		}

		public bool AllowReference(Autodesk.Revit.DB.Reference reference, Autodesk.Revit.DB.XYZ position)
		{
			return false;
		}
	}
}
