﻿using System;
using System.Collections; 
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Xml; 
using System.IO ; 

using Umbraco.Core ; 
using umbraco.cms.businesslogic;
using umbraco.cms.businesslogic.datatype;
using umbraco.cms.businesslogic.media ;
using umbraco.cms.businesslogic.propertytype;
using umbraco.DataLayer;
using umbraco.cms.businesslogic.template;
using Umbraco.Core.IO;

/* WARNING - THIS CODE CURRENTLY BORKS AN UMBRACO INSTALLATION */

namespace jumps.umbraco.usync
{
    public class SyncMediaTypes
    {
        public static void SaveToDisk(MediaType item)
        {
            XmlDocument xmlDoc = helpers.XmlDoc.CreateDoc();
            xmlDoc.AppendChild(MediaTypeHelper.ToXml(xmlDoc, item));
            helpers.XmlDoc.SaveXmlDoc(item.GetType().ToString(), item.Text, xmlDoc);
        }

        public static void SaveAllToDisk()
        {
            foreach (MediaType item in MediaType.GetAllAsList())
            {
                SaveToDisk(item);
            }
        }

        public static void ReadAllFromDisk()
        {
            string path = IOHelper.MapPath(string.Format("{0}{1}",
                helpers.uSyncIO.RootFolder,
                "umbraco.cms.businesslogic.media.MediaType"));

            ReadFromDisk(path);
        }

        public static void ReadFromDisk(string path)
        {
            // actually read it in....
            // 
            if (Directory.Exists(path))
            {
                foreach (string file in Directory.GetFiles(path, "*.config"))
                {
                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.Load(file);

                    XmlNode node = xmlDoc.SelectSingleNode("//MediaType");

                    if (node != null)
                    {
                        // do the actuall magic here...
                    }
                }
            }
        }

        public static void AttachEvents()
        {
            MediaType.AfterSave += MediaType_AfterSave;
        }

        static void MediaType_AfterSave(MediaType sender, SaveEventArgs e)
        {
            SaveToDisk(sender); 
        }
    }

    public class MediaTypeHelper
    {
        public static XmlElement ToXml(XmlDocument xd, MediaType mt)
        {
            XmlElement doc = xd.CreateElement("MediaType");

            // build the info section (name and stuff)
            XmlElement info = xd.CreateElement("Info");
            doc.AppendChild(info);

            info.AppendChild(XmlHelper.AddTextNode(xd, "Name", mt.Text));
            info.AppendChild(XmlHelper.AddTextNode(xd, "Alias", mt.Alias));
            info.AppendChild(XmlHelper.AddTextNode(xd, "Icon", mt.IconUrl));
            info.AppendChild(XmlHelper.AddTextNode(xd, "Thumbnail", mt.Thumbnail));
            info.AppendChild(XmlHelper.AddTextNode(xd, "Description", mt.Description));

            // now the Media Type Scructure
            XmlElement structure = xd.CreateElement("Structure");
            foreach(int child in mt.AllowedChildContentTypeIDs.ToList())
            {
                structure.AppendChild(XmlHelper.AddTextNode(xd, "MediaType", new MediaType(child).Alias));
            }

            // stuff in the generic properties tab
            XmlElement props = xd.CreateElement("GenericProperties");
            foreach(PropertyType pt in mt.PropertyTypes)
            {
                // we only add properties that arn't in a parent (although media types are flat at the mo)
                if (pt.ContentTypeId == mt.Id)
                {
                    XmlElement prop = xd.CreateElement("GenericProperty");
                    prop.AppendChild(XmlHelper.AddTextNode(xd, "Name", pt.Name));
                    prop.AppendChild(XmlHelper.AddTextNode(xd, "Alias", pt.Alias));
                    prop.AppendChild(XmlHelper.AddTextNode(xd, "type", pt.DataTypeDefinition.DataType.Id.ToString()));

                    prop.AppendChild(XmlHelper.AddTextNode(xd, "Definition", pt.DataTypeDefinition.UniqueId.ToString()));
                    prop.AppendChild(XmlHelper.AddTextNode(xd, "Tab", ContentType.Tab.GetCaptionById(pt.TabId)));
                    prop.AppendChild(XmlHelper.AddTextNode(xd, "Mandatory", pt.Mandatory.ToString()));
                    prop.AppendChild(XmlHelper.AddTextNode(xd, "Validation", pt.ValidationRegExp));
                    prop.AppendChild(XmlHelper.AddCDataNode(xd, "Description", pt.Description));
                    // add this property to the tree
                    props.AppendChild(prop) ; 
                }

                
            }
            // add properties to the doc
            doc.AppendChild(props) ; 
                                                
            return doc;
        }

        public static void Import(XmlNode n) 
        { 
        }
    }
}

/*
        public static void ImportMediaType(XmlNode n)
        {
             global::umbraco.BusinessLogic.User u = new global::umbraco.BusinessLogic.User(0) ; 
            // is this an existing media type ?
            MediaType mt = MediaType.GetByAlias(n.SelectSingleNode("Info/Alias").Value);
            if (mt == null)
            {
                mt = MediaType.MakeNew(u, helpers.XmlDoc.GetNodeValue(n.SelectSingleNode("Info/Name"))); 
                mt.Alias = helpers.XmlDoc.GetNodeValue(n.SelectSingleNode("Info/Alias"));
               
            }
            else {
                mt.Text = n.SelectSingleNode("Info/Name").Value ; 
            }

                    
            // Info
            mt.IconUrl = helpers.XmlDoc.GetNodeValue(n.SelectSingleNode("Info/Icon"));
            mt.Thumbnail = helpers.XmlDoc.GetNodeValue(n.SelectSingleNode("Info/Thumbnail"));
            mt.Description = helpers.XmlDoc.GetNodeValue(n.SelectSingleNode("Info/Description"));

            // Tabs
            ContentType.TabI[] tabs = mt.getVirtualTabs;
            string tabNames = ";";
            for (int t = 0; t < tabs.Length; t++)
                tabNames += tabs[t].Caption + ";";

            Hashtable ht = new Hashtable();
            foreach (XmlNode t in n.SelectNodes("Tabs/Tab"))
            {
                if (tabNames.IndexOf(";" + helpers.XmlDoc.GetNodeValue(t.SelectSingleNode("Caption")) + ";") == -1)
                {
                    ht.Add(int.Parse(helpers.XmlDoc.GetNodeValue(t.SelectSingleNode("Id"))),
                        mt.AddVirtualTab(helpers.XmlDoc.GetNodeValue(t.SelectSingleNode("Caption"))));
                }
            }

            mt.ClearVirtualTabs();
            // Get all tabs in hashtable
            Hashtable tabList = new Hashtable();
            foreach (ContentType.TabI t in mt.getVirtualTabs.ToList())
            {
                if (!tabList.ContainsKey(t.Caption))
                    tabList.Add(t.Caption, t.Id);
            }

            // Generic Properties
            global::umbraco.cms.businesslogic.datatype.controls.Factory f = 
                new global::umbraco.cms.businesslogic.datatype.controls.Factory();
            foreach (XmlNode gp in n.SelectNodes("GenericProperties/GenericProperty"))
            {
                int dfId = 0;
                Guid dtId = new Guid(helpers.XmlDoc.GetNodeValue(gp.SelectSingleNode("Type")));

                if (gp.SelectSingleNode("Definition") != null && !string.IsNullOrEmpty(helpers.XmlDoc.GetNodeValue(gp.SelectSingleNode("Definition"))))
                {
                    Guid dtdId = new Guid(helpers.XmlDoc.GetNodeValue(gp.SelectSingleNode("Definition")));
                    if (CMSNode.IsNode(dtdId))
                        dfId = new CMSNode(dtdId).Id;
                }
                if (dfId == 0)
                {
                    try
                    {
                        dfId = findDataTypeDefinitionFromType(ref dtId);
                    }
                    catch
                    {
                        throw new Exception(String.Format("Could not find datatype with id {0}.", dtId));
                    }
                }

                // Fix for rich text editor backwards compatibility 
                if (dfId == 0 && dtId == new Guid("a3776494-0574-4d93-b7de-efdfdec6f2d1"))
                {
                    dtId = new Guid("83722133-f80c-4273-bdb6-1befaa04a612");
                    dfId = findDataTypeDefinitionFromType(ref dtId);
                }

                if (dfId != 0)
                {
                    PropertyType pt = mt.getPropertyType(helpers.XmlDoc.GetNodeValue(gp.SelectSingleNode("Alias")));
                    if (pt == null)
                    {
                        mt.AddPropertyType(
                            global::umbraco.cms.businesslogic.datatype.DataTypeDefinition.GetDataTypeDefinition(dfId),
                            helpers.XmlDoc.GetNodeValue(gp.SelectSingleNode("Alias")),
                            helpers.XmlDoc.GetNodeValue(gp.SelectSingleNode("Name"))
                            );
                        pt = mt.getPropertyType(helpers.XmlDoc.GetNodeValue(gp.SelectSingleNode("Alias")));
                    }
                    else
                    {
                        pt.DataTypeDefinition = global::umbraco.cms.businesslogic.datatype.DataTypeDefinition.GetDataTypeDefinition(dfId);
                        pt.Name = helpers.XmlDoc.GetNodeValue(gp.SelectSingleNode("Name"));
                    }

                    pt.Mandatory = bool.Parse(helpers.XmlDoc.GetNodeValue(gp.SelectSingleNode("Mandatory")));
                    pt.ValidationRegExp = helpers.XmlDoc.GetNodeValue(gp.SelectSingleNode("Validation"));
                    pt.Description = helpers.XmlDoc.GetNodeValue(gp.SelectSingleNode("Description"));

                    // tab
                    try
                    {
                        if (tabList.ContainsKey(helpers.XmlDoc.GetNodeValue(gp.SelectSingleNode("Tab"))))
                            pt.TabId = (int)tabList[helpers.XmlDoc.GetNodeValue(gp.SelectSingleNode("Tab"))];
                    }
                    catch (Exception ee)
                    {
                        global::umbraco.BusinessLogic.Log.Add(global::umbraco.BusinessLogic.LogTypes.Error, null, mt.Id, "Packager: Error assigning property to tab: " + ee.ToString());
                    }

                    mt.Save(); 
                }
            }
        }

        private static int findDataTypeDefinitionFromType(ref Guid dtId)
        {
            int dfId = 0;
            foreach (global::umbraco.cms.businesslogic.datatype.DataTypeDefinition df in global::umbraco.cms.businesslogic.datatype.DataTypeDefinition.GetAll())
                if (df.DataType.Id == dtId)
                {
                    dfId = df.Id;
                    break;
                }
            return dfId;
        }
    }
    
}
*/