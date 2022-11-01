using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace GloryHoleCutter
{
    public class GloryHoleCutterSettings
    {
        public string UseSleevesForRoundHolesButtonName { get; set; }
        public GloryHoleCutterSettings GetSettings()
        {
            GloryHoleCutterSettings gloryHoleCutterSettings = null;
            string assemblyPathAll = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string fileName = "GloryHoleCutterSettings.xml";
            string assemblyPath = assemblyPathAll.Replace("GloryHoleCutter.dll", fileName);

            if (File.Exists(assemblyPath))
            {
                using (FileStream fs = new FileStream(assemblyPath, FileMode.Open))
                {
                    XmlSerializer xSer = new XmlSerializer(typeof(GloryHoleCutterSettings));
                    gloryHoleCutterSettings = xSer.Deserialize(fs) as GloryHoleCutterSettings;
                    fs.Close();
                }
            }
            else
            {
                gloryHoleCutterSettings = new GloryHoleCutterSettings();
            }

            return gloryHoleCutterSettings;
        }

        public void SaveSettings()
        {
            string assemblyPathAll = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string fileName = "GloryHoleCutterSettings.xml";
            string assemblyPath = assemblyPathAll.Replace("GloryHoleCutter.dll", fileName);

            if (File.Exists(assemblyPath))
            {
                File.Delete(assemblyPath);
            }

            using (FileStream fs = new FileStream(assemblyPath, FileMode.Create))
            {
                XmlSerializer xSer = new XmlSerializer(typeof(GloryHoleCutterSettings));
                xSer.Serialize(fs, this);
                fs.Close();
            }
        }
    }
}
