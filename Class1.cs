using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsInterface;
using Autodesk.AutoCAD.GraphicsSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using System.Collections;

namespace FilterByBlockName
{
    public class DumpAttributes
    {

        [CommandMethod("TATT")]
        public void ListAttributes()
        {

            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;

            Database db = HostApplicationServices.WorkingDatabase;

            Transaction tr = db.TransactionManager.StartTransaction();



            try
            {
                TypedValue[] acTypValAr = new TypedValue[1];
                acTypValAr.SetValue(new TypedValue((int)DxfCode.Start, "INSERT"), 0);
                var filter = new SelectionFilter(acTypValAr);

                PromptSelectionOptions opts = new PromptSelectionOptions();

                opts.MessageForAdding = "Select block references: ";
                PromptSelectionResult res = ed.GetSelection(opts, filter);



                if (res.Status != PromptStatus.OK)
                {
                    ed.WriteMessage("\nNo block references selected");
                    return;
                }

                SelectionSet selSet = res.Value;
                ObjectId[] idArray = selSet.GetObjectIds();

                Dictionary<string, int> tableInfo = new Dictionary<string, int>();

                foreach (ObjectId blkId in idArray)
                {
                    BlockReference blkRef = (BlockReference)tr.GetObject(blkId, OpenMode.ForRead);
                    string name = blkRef.Name;

                    if (tableInfo.ContainsKey(name))
                    {
                        tableInfo[name]++;
                    }
                    else
                    {
                        tableInfo.Add(name, 1);
                    }

                }

                foreach (KeyValuePair<string, int> entry in tableInfo)
                {
                    ed.WriteMessage($"Block: {entry.Key} is repeated {entry.Value} times\n");
                }

                tr.Commit();
            }
            catch (Autodesk.AutoCAD.Runtime.Exception ex)
            {
                ed.WriteMessage(("Exception: " + ex.Message));
            }
            finally
            {
                tr.Dispose();
            }
        }

        [CommandMethod("TATB")]
        public void DrawBlock()
        {
            Document cDoc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = cDoc.Editor;
            Database cDB = cDoc.Database;

            string name = "";
            using (Transaction Trans1 = cDB.TransactionManager.StartTransaction())
            {
                PromptStringOptions options = new PromptStringOptions("Enter block name: ");
                PromptResult pr = ed.GetString(options);
                name = pr.StringResult;
            }

            InsertBlock(new Point3d(0, 0, 0), name);
        }


        public void InsertBlock(Point3d InsPt, string BlkName)
        {
            Document cDoc = Application.DocumentManager.MdiActiveDocument;
            Database cDB = cDoc.Database;
            Editor cEd = cDoc.Editor;

            using (Transaction Trans1 = cDB.TransactionManager.StartTransaction())
            {
                ObjectId blockId = cDB.CurrentSpaceId;
                BlockTableRecord btr = Trans1.GetObject(blockId, OpenMode.ForWrite) as BlockTableRecord;
                BlockTable blkTable = Trans1.GetObject(cDB.BlockTableId, OpenMode.ForWrite) as BlockTable;
                BlockTableRecord blkTableRec = new BlockTableRecord();
                blkTableRec.Name = BlkName;
                if (!blkTable.Has(BlkName))
                {
                    blkTable.Add(blkTableRec);
                    Trans1.AddNewlyCreatedDBObject(blkTableRec, true);
                }
                
                ObjectId id = blkTable[BlkName];
                // Insert support block at insertion point InsPt
                BlockReference rfml = new BlockReference(InsPt, id);
                btr.AppendEntity(rfml);

                Trans1.AddNewlyCreatedDBObject(rfml, true);

                Trans1.Commit();
            }
        }
    }
}
